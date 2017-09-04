using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class LogBuffer
{
    public const int KB = 1024;
    public const int InternalBufSize = 16 * KB;

    public byte[] Buf;
    public int BufSize = InternalBufSize;
    public int BufWrittenBytes = 0;

    public LogBuffer(int userDefinedSize = 0)
    {
        if (userDefinedSize != 0)
            BufSize = userDefinedSize;

        Buf = new byte[BufSize];
    }

    public bool Receive(string content)
    {
        byte[] bytes = Encoding.Default.GetBytes(content);
        if (BufWrittenBytes + bytes.Length > BufSize)
            return false;

        Buffer.BlockCopy(bytes, 0, Buf, BufWrittenBytes, bytes.Length);
        BufWrittenBytes += bytes.Length;
        return true;
    }

    public void Clear()
    {
        BufWrittenBytes = 0;
    }
}


public class LogEventArgs : EventArgs
{
    public LogEventArgs(int seqID, LogType type, string content, string stacktrace, float time)
    {
        SeqID = seqID;
        LogType = type;
        Content = content;
        Stacktrace = stacktrace;
        Time = time;
    }

    public int SeqID = 0;
    public LogType LogType;
    public string Content = "";
    public string Stacktrace = "";
    public float Time = 0.0f;
}

public delegate void LogTargetHandler(object sender, LogEventArgs args);

public class LogService : IDisposable
{
    public event LogTargetHandler LogTargets;

    public static int UserDefinedMemBufSize = 0;

    public LogService(bool logIntoFile, int oldLogsKeptDays, bool useMemBuf) // '-1' means keeping all logs without any erasing
    {
        RegisterCallback();

        _useMemBuf = useMemBuf;

        _memBuf = new LogBuffer(UserDefinedMemBufSize);

        if (logIntoFile)
        {
            try
            {
                DateTime dt = DateTime.Now;

                string logDir = LogUtil.CombinePaths(Application.persistentDataPath, "log", LogUtil.FormatDateAsFileNameString(dt));
                Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, LogUtil.FormatDateAsFileNameString(dt) + '_' + LogUtil.FormatTimeAsFileNameString(dt) + ".txt");

                _logWriter = new FileInfo(logPath).CreateText();
                _logWriter.AutoFlush = true;
                _logPath = logPath;

                Log.Info("'Log Into File' enabled, file opened successfully. ('{0}')", _logPath);
                LastLogFile = _logPath;
            }
            catch (System.Exception ex)
            {
                Log.Info("'Log Into File' enabled but failed to open file.");
                Log.Exception(ex);
            }
        }
        else
        {
            Log.Info("'Log Into File' disabled.");
            LastLogFile = "";
        }

        if (oldLogsKeptDays > 0)
        {
            try
            {
                CleanupLogsOlderThan(oldLogsKeptDays);
            }
            catch (Exception e)
            {
                Log.Exception(e);
                Log.Error("Cleaning up logs ({0}) failed.", oldLogsKeptDays);
            }
        }
    }
    public bool UseMemBuf
    {
        get { return _useMemBuf; }
        set { _useMemBuf = value; FlushLogWriting(); }
    }

    public void Dispose()
    {
        FlushLogWriting();

        Log.Info("destroying log service...");

        if (_logWriter != null)
        {
            _logWriter.Close();
        }

#if UNITY_5_0
        Application.logMessageReceivedThreaded -= OnLogReceived;
#endif

        _disposed = true;
    }

    public void WriteLog(string content, LogType type)
    {
        // save it in memory first
        if (LogUtil.EnableInMemoryStorage)
        {
            switch (type)
            {
            case LogType.Error:
                LogUtil.PushInMemoryError(content);
                break;
            case LogType.Exception:
                LogUtil.PushInMemoryException(content);
                break;
            default:
                break;
            }
        }

        if (_useMemBuf)
        {
            // write directly if it's error or message is larger than buffer
            if (type == LogType.Error || Encoding.Default.GetByteCount(content) > _memBuf.BufSize)
            {
                // flush into file before writing
                FlushMemBuffer();

                if (_logWriter != null)
                {
                    _logWriter.Write(content);
                }
            }
            else if (!_memBuf.Receive(content))
            {
                // flush into file when buffer is full
                FlushMemBuffer();

                _memBuf.Receive(content);
            }
        }
        else
        {
            if (_logWriter != null)
            {
                _logWriter.Write(content);
            }
        }
    }

    public void FlushLogWriting()
    {
        FlushFoldedMessage();
    }

    private void CleanupLogsOlderThan(int days)
    {
        DateTime timePointForDeleting = DateTime.Now.Subtract(TimeSpan.FromDays(days));
        string timeStrForDeleting = LogUtil.FormatDateAsFileNameString(timePointForDeleting);

        DirectoryInfo logDirInfo = new DirectoryInfo(LogUtil.CombinePaths(Application.persistentDataPath, "log"));
        DirectoryInfo[] dirsByDate = logDirInfo.GetDirectories();
        List<string> toBeDeleted = new List<string>();
        foreach (var item in dirsByDate)
        {
            //Log.Info("[COMPARING]: {0}, {1}", item.Name, timeStrForDeleting);
            if (string.CompareOrdinal(item.Name, timeStrForDeleting) <= 0)
            {
                toBeDeleted.Add(item.FullName);
                //Log.Info("[TO_BE_DELETED]: {0}", item.FullName);
            }
        }

        foreach (var item in toBeDeleted)
        {
            Directory.Delete(item, true);
            Log.Info("[ Log Cleanup ]: {0}", item);
        }
    }

    private void RegisterCallback()
    {
        Application.logMessageReceivedThreaded += OnLogReceived;
    }

    private void WriteTrace(string content)
    {
        OnLogReceived(content, "", LogType.Log);
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        if (_disposed)
            throw new Exception(string.Format("LogService used after being disposed. (content:{0})", condition));

        if (_reentranceGuard)
            throw new Exception(string.Format("LogService Reentrance occurred. (content:{0})", condition));

        _reentranceGuard = true;

        ++_seqID;

        switch (type)
        {
        case LogType.Assert:
            _assertCount++;
            break;
        case LogType.Error:
            _errorCount++;
            break;
        case LogType.Exception:
            _exceptionCount++;
            break;

        case LogType.Warning:
        case LogType.Log:
        default:
            break;
        }

        try
        {
            // 如果是异常的话，加上堆栈信息
            if (type == LogType.Exception)
            {
                condition = string.Format("{0}\r\n  {1}", condition, stackTrace.Replace("\n", "\r\n  "));
            }

            if (condition == _lastWrittenContent)
            {
                _foldedCount++;
            }
            else
            {
                FlushFoldedMessage();

                WriteLog(string.Format("{0:0.00} {1}: {2}\r\n", Time.realtimeSinceStartup, type, condition), type);

                _lastWrittenContent = condition;
                _lastWrittenType = type;
            }

            if (LogTargets != null)
            {
                foreach (LogTargetHandler Caster in LogTargets.GetInvocationList())
                {
                    ISynchronizeInvoke SyncInvoke = Caster.Target as ISynchronizeInvoke;
                    LogEventArgs args = new LogEventArgs(_seqID, type, condition, stackTrace, Time.realtimeSinceStartup);

                    if (SyncInvoke != null && SyncInvoke.InvokeRequired)
                        SyncInvoke.Invoke(Caster, new object[] { this, args });
                    else
                        Caster(this, args);
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Exception(ex); // this should at least print to Unity Editor (but may skip the file writing due to earlier writing failure)             
        }
        finally
        {
            _reentranceGuard = false;
        }
    }

    private void FlushMemBuffer()
    {
        if (!_useMemBuf)
            return;

        if (_logWriter != null && _memBuf.BufWrittenBytes > 0)
        {
            _logWriter.Write(Encoding.Default.GetString(_memBuf.Buf, 0, _memBuf.BufWrittenBytes));
        }
        _memBuf.Clear();
    }

    private void FlushFoldedMessage()
    {
        FlushMemBuffer();

        if (_foldedCount > 0)
        {
            if (_logWriter != null)
            {
                _logWriter.Write(string.Format("{0:0.00} {1}: --<< folded {2} messages >>--\r\n", Time.realtimeSinceStartup, _lastWrittenType, _foldedCount));
            }

            _foldedCount = 0;
        }
    }

    private string _logPath;
    private StreamWriter _logWriter;

    private ushort _seqID = 0;
    private int _assertCount = 0;
    private int _errorCount = 0;
    private int _exceptionCount = 0;

    private bool _disposed = false;

    private bool _useMemBuf = true;
    private LogBuffer _memBuf;
    private string _lastWrittenContent;
    private LogType _lastWrittenType;
    private int _foldedCount = 0;

    private bool _reentranceGuard = false;

    public static string LastLogFile { get; set; }
}
