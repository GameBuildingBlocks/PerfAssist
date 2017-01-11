using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public enum ResourceRequestType
{
    Ordinary,
    Async,
}

public class ResourceRequestInfo
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

    public override string ToString()
    {
        return string.Format("#{0} ({1:0.000}) {2} {3} {4} +{5} +{6} ({7})",
            seqID, requestTime, rootID, resourceType.ToString(), 
            requestType == ResourceRequestType.Async ? "(a)" : "", resourcePath, srcFile, srcLineNum);
    }

    public void RecordObject(UnityEngine.Object obj)
    {
        rootID = obj.GetInstanceID();
        resourceType = obj.GetType();
    }
}

public class ResourceTracker : IDisposable
{
    public static ResourceTracker Instance;

    // this boolean is *read-only* after the instance is created
    public bool EnableTracking { get { return _enableTracking; } }
    private bool _enableTracking = false;    

    private StreamWriter _logWriter = null;
    //private string _logPath = "";
    private int _reqSeq = 0;

    public Dictionary<string, string> ShaderPropertyDict
    {
        get { return _shaderPropertyDict;}
    }
    private Dictionary<string, string> _shaderPropertyDict = null;

    public ResourceTracker(bool enableTracking)
    {
        if (enableTracking)
        {
            Open();

            if (_enableTracking)
            {
                if (UsNet.Instance != null && UsNet.Instance.CmdExecutor != null)
                    UsNet.Instance.CmdExecutor.RegisterHandler(eNetCmd.CL_RequestStackData, NetHandle_RequestStackData);
                else
                    UnityEngine.Debug.LogError("UsNet not available");

                readShaderPropertyJson();
            }
        }
    }

    private void readShaderPropertyJson()
    {
        if (_shaderPropertyDict == null)
        {
            try
            {
                // 临时注掉，这里考虑用 UnityEngine.JsonUtility
                //StreamReader sr = new StreamReader(new FileStream(ResourceTrackerConst.shaderPropertyNameJsonPath, FileMode.Open));
                //string jsonStr = sr.ReadToEnd();
                //sr.Close();
                //var jsonData = new JsonReader(jsonStr);
                //_shaderPropertyDict = JsonMapper.ToObject<Dictionary<string, string>>(jsonData);
            }
            catch (System.Exception)
            {
                UnityEngine.Debug.Log("no ShaderPropertyNameRecord.json");
            }
        }
    }


    public void Open()
    {
        if (_enableTracking)
        {
            UnityEngine.Debug.LogFormat("[ResourceTracker] info: {0} ", "already enabled, ignored.");
            return;
        }

        try
        {
            string logDir = Path.Combine(Application.persistentDataPath, "mem_logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            DateTime dt = DateTime.Now;

            string logFile = string.Format("{0}_{1}_alloc.txt", SysUtil.FormatDateAsFileNameString(dt), SysUtil.FormatTimeAsFileNameString(dt));
            string logPath = Path.Combine(logDir, logFile);

            _logWriter = new FileInfo(logPath).CreateText();
            _logWriter.AutoFlush = true;
            //_logPath = logPath;

            _enableTracking = true;
            UnityEngine.Debug.LogFormat("[ResourceTracker] tracking enabled: {0} ", logPath);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogErrorFormat("[ResourceTracker] Open() failed, error: {0} ", ex.Message);

            if (_logWriter != null)
            {
                _logWriter.Close();
                _logWriter = null;
            }

            _enableTracking = false;
            //_logPath = "";
        }
    }

    public void Close()
    {
        if (_logWriter != null)
        {
            try
            {
                _logWriter.WriteLine("--------- unfinished request: {0} --------- ", InProgressAsyncObjects.Count);
                foreach (KeyValuePair<System.Object, ResourceRequestInfo> p in InProgressAsyncObjects)
                {
                    _logWriter.WriteLine("  + {0}", p.Value.ToString());
                }
                _logWriter.Close();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogErrorFormat("[ResourceTracker.ctor] error: {0} ", ex.Message);
            }
            finally
            {
                _logWriter = null;
            }
        }

        _enableTracking = false;
    }

    public void Dispose()
    {
        if (_enableTracking && _logWriter != null)
        {
            Close();
        }
    }

    public void TrackSyncRequest(UnityEngine.Object spawned, string path)
    {
        if (!_enableTracking)
            return;

        var sf = new System.Diagnostics.StackFrame(2, true);
        var request = NewRequest(path, sf);
        request.requestType = ResourceRequestType.Ordinary;
        TrackRequestWithObject(request, spawned);
    }

    public void TrackResourcesDotLoad(UnityEngine.Object loaded, string path)
    {
        if (!_enableTracking)
            return;

        var sf = new System.Diagnostics.StackFrame(1, true);
        var request = NewRequest(path, sf);
        request.requestType = ResourceRequestType.Ordinary;
        TrackRequestWithObject(request, loaded);
    }

    public void TrackAsyncRequest(System.Object handle, string path)
    {
        if (!_enableTracking)
            return;

        var sf = new System.Diagnostics.StackFrame(2, true);
        if (sf.GetMethod().Name.Contains("SpawnAsyncOldVer"))
        {
            sf = new System.Diagnostics.StackFrame(3, true);
        }

        InProgressAsyncObjects[handle] = NewRequest(path, sf);
    }

    public void TrackAsyncDone(System.Object handle, UnityEngine.Object target)
    {
        if (!_enableTracking)
            return;

        ResourceRequestInfo request;
        if (!InProgressAsyncObjects.TryGetValue(handle, out request))
            return;

        request.requestType = ResourceRequestType.Async;
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
            TrackSyncRequest(roots[i], "[scene]: " + sceneName);
        }
    }

    public void TrackObjectInstantiation(UnityEngine.Object src, UnityEngine.Object instantiated)
    {
        if (!_enableTracking)
            return;

        int allocSeqID = -1;
        if (!TrackedGameObjects.TryGetValue(src.GetInstanceID(), out allocSeqID))
            return;

        ExtractObjectResources(instantiated, allocSeqID);
    }

    public ResourceRequestInfo GetAllocInfo(int instID, string className)
    {
        if (!_enableTracking)
            return null;

        int allocSeqID = -1;
        if (className == "GameObject")
        {
            if (!TrackedGameObjects.TryGetValue(instID, out allocSeqID))
                return null;
        }
        else if (SceneGraphExtractor.MemCategories.Contains(className))
        {
            if (!TrackedMemObjects.TryGetValue(instID, out allocSeqID))
                return null;
        }
        
        ResourceRequestInfo requestInfo = null;
        if (!TrackedAllocInfo.TryGetValue(allocSeqID, out requestInfo))
            return null;

        return requestInfo;
    }

    public string GetStackTrace(ResourceRequestInfo req)
    {
        string stacktrace;
        if (!Stacktraces.TryGetValue(req.stacktraceHash, out stacktrace))
            return "";

        return stacktrace;
    }

    private ResourceRequestInfo NewRequest(string path, StackFrame sf)
    {
        ResourceRequestInfo reqInfo = new ResourceRequestInfo();
        reqInfo.resourcePath = path;
        reqInfo.srcFile = sf.GetFileName();
        reqInfo.srcLineNum = sf.GetFileLineNumber();
        reqInfo.seqID = _reqSeq++;

        string stacktrace = UnityEngine.StackTraceUtility.ExtractStackTrace();

        int _tryCount = 10;
        while (_tryCount > 0)
        {
            string stacktraceStored;
            if (!Stacktraces.TryGetValue(stacktrace.GetHashCode(), out stacktraceStored))
            {
                Stacktraces[stacktrace.GetHashCode()] = stacktrace;
                break;
            }
            else
            {
                if (stacktrace == stacktraceStored)
                {
                    break;
                }
                else
                {
                    // collision happens!
                    stacktrace += ((int)(UnityEngine.Random.value * 100)).ToString();
                }
            }

            _tryCount--;
        }

        reqInfo.stacktraceHash = stacktrace.GetHashCode();
        return reqInfo;
    }

    private void ExtractObjectResources(UnityEngine.Object obj, int reqSeqID)
    {
        SceneGraphExtractor sge = new SceneGraphExtractor(obj);

        for (int i = 0; i < sge.GameObjectIDs.Count; i++)
        {
            if (!TrackedGameObjects.ContainsKey(sge.GameObjectIDs[i]))
            {
                TrackedGameObjects[sge.GameObjectIDs[i]] = reqSeqID;
            }
        }

        foreach (var p in sge.MemObjectIDs)
        {
            foreach (var item in p.Value)
            {
                if (!TrackedMemObjects.ContainsKey(item))
                {
                    TrackedMemObjects[item] = reqSeqID;
                }
            }
        }
    }

    public bool NetHandle_RequestStackData(eNetCmd cmd, UsCmd c)
    {
        int instanceID = c.ReadInt32();
        string className = c.ReadString();
        UnityEngine.Debug.Log(string.Format("NetHandle_RequestStackData instanceID={0} className={1}", instanceID, className));

        ResourceRequestInfo requestInfo = ResourceTracker.Instance.GetAllocInfo(instanceID, className);

        UsCmd pkt = new UsCmd();
        pkt.WriteNetCmd(eNetCmd.SV_QueryStacksResponse);
        if (requestInfo == null)
            pkt.WriteString("<no_callstack_available>");
        else
            pkt.WriteString(ResourceTracker.Instance.GetStackTrace(requestInfo));
        UsNet.Instance.SendCommand(pkt);
        return true;
    }


    private void TrackRequestWithObject(ResourceRequestInfo req, UnityEngine.Object obj)
    {
        try
        {
            req.RecordObject(obj);

            TrackedAllocInfo[req.seqID] = req;
            ExtractObjectResources(obj, req.seqID);

            if (_logWriter != null)
                _logWriter.WriteLine(req.ToString());
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogErrorFormat("[ResourceTracker.TrackAsyncDone] error: {0} \n {1} \n {2}",
                ex.Message, req != null ? req.ToString() : "", ex.StackTrace);
        }
    }

    Dictionary<System.Object, ResourceRequestInfo> InProgressAsyncObjects = new Dictionary<System.Object, ResourceRequestInfo>();
    Dictionary<int, ResourceRequestInfo> TrackedAllocInfo = new Dictionary<int, ResourceRequestInfo>();

    Dictionary<int, int> TrackedGameObjects = new Dictionary<int, int>();
    Dictionary<int, int> TrackedMemObjects = new Dictionary<int, int>();

    Dictionary<int, string> Stacktraces = new Dictionary<int, string>();
}
