 using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEngine;

public class AssetStatsConst
{
    public static string UploadURL = "http://jx3mr.rdev.kingsoft.net/resourceAnalysisPackageUseUpload";
    //public static string UploadURL_Test = "http://10.11.130.4:88/resourceAnalysisPackageUseUpload";
}

public class AssetRequestInfo
{
    public int seqID = 0;
    public int rootID = 0;

    public ResourceRequestType requestType;

    public string resourcePath = "";
    public System.Type resourceType = null;

    public string srcFile = "";
    public int srcLineNum = 0;

    public double requestTime = Time.realtimeSinceStartup;

    public int stacktraceHash = 0;

	public double duration = 0.0;
	public bool isAsync = false;

    public override string ToString()
    {
        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return string.Format("{0:0.00} {1} {2} {3} {4:0.00}ms {5}",
            requestTime,
            resourceType != null ? resourceType.ToString() : "<null_type>",
            resourcePath,
            scene.name,
			duration,
			requestType == ResourceRequestType.Async ? "async": "sync");
    }

    class LuaAsset : UnityEngine.Object { public static LuaAsset Instance = new LuaAsset(); }

    public void RecordObject(UnityEngine.Object obj)
    {
        if (obj.name == "LuaAsset")
        {
            rootID = -1;
            resourceType = LuaAsset.Instance.GetType();
        }
        else
        {
            rootID = obj.GetInstanceID();
            resourceType = obj.GetType();
        }
    }
}

public class AssetUsageStats : IDisposable
{
    public static AssetUsageStats Instance;

    // this boolean is *read-only* after the instance is created
    public bool EnableTracking { get { return _enableTracking; } }
    private bool _enableTracking = false;

    private StreamWriter _logWriter = null;
    //private int _reqSeq = 0;

    private DateTime _lastWriteTime = DateTime.Now;

    private string _logDir = "";

    void LogInfo(string info)
    {
        UnityEngine.Debug.LogFormat("[AssetUsageStats] (info): {0} ", info);
    }

    void LogError(string err)
    {
        UnityEngine.Debug.LogErrorFormat("[AssetUsageStats] (err): {0} ", err);
    }

    public AssetUsageStats(bool enableTracking)
    {
        _enableTracking = enableTracking;

        if (_enableTracking)
        {
            try
            {
                _logDir = Path.Combine(Application.persistentDataPath, "asset_stats");
                if (!Directory.Exists(_logDir))
                    Directory.CreateDirectory(_logDir);
            }
            catch (System.Exception ex)
            {
                _logDir = "";
                _enableTracking = false;
                LogError(ex.Message);
                LogError("Failed to prepare the stats dir, aborted.");
            }

            // UploadFiles();
        }
    }

    public bool PrepareWriter()
    {
        if (!_enableTracking)
            return false;

        DateTime dt = DateTime.Now;
        if (_logWriter != null)
        {
            if (dt.Hour == _lastWriteTime.Hour && dt.Minute / 10 == _lastWriteTime.Minute / 10)
            {
                return true; // nothing to do, keep writing
            } 
            else
            {
                LogInfo("Switching file at: " + dt.ToString());
                CloseWriter();
            }
        }

        // create a new text to write
        try
        {
            string baseInfo = Application.isEditor ? "editor" : "-";
            string svnVersion = "000";
            string buildInfoID = "000";

#if JX3M
            if (!Application.isEditor)
                svnVersion = Lua2CS.GetVersion();
            if (!Application.isEditor)
                buildInfoID = Lua2CS.BuildInfoID();
#endif

            string logFile = string.Format("{0}_{1}-{2}_{3}_{4}.txt", baseInfo, 
                SysUtil.FormatDateAsFileNameString(dt), 
                SysUtil.FormatTimeAsFileNameString(dt), svnVersion,buildInfoID);
            string filePath = Path.Combine(_logDir, logFile);

            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
                LogInfo("Creating new text successfully at: " + filePath);
            }

            if (_logWriter == null)
            {
                _logWriter = new StreamWriter(filePath, true);
                _logWriter.AutoFlush = true;   // TODO: buffering for better performance
            }
            return true;
        }
        catch (Exception ex)
        {
            _enableTracking = false;
            LogError("Creating new text failed: " + ex.Message);
            CloseWriter();
            return false;
        }
    }

    public void CloseWriter()
    {
        if (_logWriter != null)
        {
            try
            {
                _logWriter.Close();
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
            }
            finally
            {
                _logWriter = null;
                LogInfo("Writer closed.");

                // UploadFiles();	don't upload anymore
            }
        }
    }

    public void Dispose()
    {
        if (_enableTracking && _logWriter != null)
        {
            CloseWriter();
            _enableTracking = false; // disposed would act as 'disabled'
        }
    }

    private GameObject _luaAsset = new GameObject("LuaAsset");
    public void TrackLuaRequest(string path, int bytes, bool loadFromCache)
    {
        if (!_enableTracking)
            return;

        LoadingStats.Instance.LogLua(path, bytes, loadFromCache);

        var request = NewRequest(path/*, sf*/);
        request.requestType = ResourceRequestType.Ordinary;
        TrackRequestWithObject(request, _luaAsset);
    }

    Stopwatch m_syncTimer = new Stopwatch();

    public void TrackSyncStartTiming()
    {
        if (!_enableTracking)
            return;

        m_syncTimer.Reset();
        m_syncTimer.Start();
    }

    public double TrackSyncStopTiming(string path)
    {
        if (!_enableTracking)
            return 0.0f;

        m_syncTimer.Stop();
        double ms = (double)m_syncTimer.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        LoadingStats.Instance.LogSync(path, ms);

		return ms;
    }

    public void TrackSyncRequest(UnityEngine.Object spawned, string path)
    {
        if (!_enableTracking)
            return;

        double ms = TrackSyncStopTiming(path);

        var request = NewRequest(path/*, sf*/);
		request.duration = ms;
        request.requestType = ResourceRequestType.Ordinary;
        TrackRequestWithObject(request, spawned);
    }

#if JX3M
    public void TrackSyncRequest(UnityEngine.Object spawned, ulong hashPath)
    {
        if (!_enableTracking)
            return;

        TrackSyncRequest(spawned, GameResource.ResourceMainfest.GetHashPath(hashPath));
    }
#endif

    public void TrackResourcesDotLoad(UnityEngine.Object loaded, string path)
    {
        if (!_enableTracking)
            return;

        double ms = TrackSyncStopTiming(path);

        var request = NewRequest(path/*, sf*/);
		request.duration = ms;
        request.requestType = ResourceRequestType.Ordinary;
        TrackRequestWithObject(request, loaded);
    }

    public void TrackAsyncRequest(System.Object handle, string path)
    {
        if (!_enableTracking)
            return;

        InProgressAsyncObjects[handle] = NewRequest(path/*, sf*/);
    }

#if JX3M
    public void TrackAsyncRequest(System.Object handle, ulong hashPath)
    {
        if (!_enableTracking)
            return;

        InProgressAsyncObjects[handle] = NewRequest(GameResource.ResourceMainfest.GetHashPath(hashPath)/*, sf*/);
    }
#endif

    public void TrackAsyncDone(System.Object handle, UnityEngine.Object target)
    {
        if (!_enableTracking || target == null)
            return;

        AssetRequestInfo request;
        if (!InProgressAsyncObjects.TryGetValue(handle, out request))
            return;

        request.requestType = ResourceRequestType.Async;
		request.duration = 0.0f;

#if JX3M
        ResourceHandle h = handle as ResourceHandle;
        if (h != null)
        {
			request.duration = (Time.time - h.StartTime) * 1000.0f;
            LoadingStats.Instance.LogAsync(request.resourcePath, request.duration);
        }
#else
        LoadingStats.Instance.LogAsync(request.resourcePath, 0.0f);
#endif

        TrackRequestWithObject(request, target);
        InProgressAsyncObjects.Remove(handle);
    }

    public void TrackSceneLoaded(string sceneName)
    {
        if (!_enableTracking)
            return;

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            TrackSyncRequest(roots[i], sceneName + ".unity");
        }
    }

    private AssetRequestInfo NewRequest(string path/*, StackFrame sf*/)
    {
        AssetRequestInfo reqInfo = new AssetRequestInfo();
        reqInfo.resourcePath = path;
        return reqInfo;
    }

    private void TrackRequestWithObject(AssetRequestInfo req, UnityEngine.Object obj)
    {
        if (obj == null || !_enableTracking || !PrepareWriter())
            return;

		if (!LoadingStats.Instance.enabled)
			return;

        try
        {
            req.RecordObject(obj);

            string info = req.ToString();
            if (_logWriter != null && !string.IsNullOrEmpty(info) && req.duration >= 1.0f)
                _logWriter.WriteLine(info);

            _lastWriteTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogErrorFormat("[ResourceTracker.TrackRequestWithObject] error: {0} \n {1} \n {2}",
                ex.Message, req != null ? req.ToString() : "", ex.StackTrace);
        }
    }

    static void ThreadedUploading(object obj)
    {
        AssetUsageStats aus = obj as AssetUsageStats;
        if (aus == null)
            return;

#if JX3M
#if TENCENT_CHANNEL
#else
        try
        {
            string[] todo = Directory.GetFiles(aus._logDir);
            string[] files = U3DPerfProfiler.HttpClient.UpdateFiles(AssetStatsConst.UploadURL, todo);
            foreach (var f in files)
            {
                try
                {
                    File.Delete(f);
                }
                catch (System.Exception ex)
                {
                    aus.LogError(string.Format("Deleting file ({0}) failed: {1}", f, ex.Message));
                }
            }

            aus.LogInfo(string.Format("{0} files are uploaded and deleted successfully.", files.Length));
        }
        catch (Exception ex)
        {
            aus.LogError("Uploading failed: " + ex.Message);
        }
#endif
#endif
    }

    private void UploadFiles()
    {
        Thread t = new Thread(ThreadedUploading);
        t.Start(this);
        t.Join();
    }

    Dictionary<System.Object, AssetRequestInfo> InProgressAsyncObjects = new Dictionary<System.Object, AssetRequestInfo>();
}
