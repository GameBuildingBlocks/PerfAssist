using AClockworkBerry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class LoadingStats : MonoBehaviour
{
    public static LoadingStats Instance = null;

    void Awake ()
    {
        ScreenLogger.Instance.enabled = false;
        ScreenLogger.Instance.LogMessages = false;
        ScreenLogger.Instance.LogWarnings = false;
        ScreenLogger.Instance.LogErrors = false;
    }

    void OnEnable()
    {
        ScreenLogger.Instance.enabled = true;
        ScreenLogger.Instance.Clear();

#if JX3M && UNITY_EDITOR
        LuaLoader.Instance.UseCache = true;
#endif
    }

    void OnDisable()
    {
        // the instance should be protected against null since this method 
        // would be called when the whole game is being destroyed
        if (ScreenLogger.Instance != null)
        {
            ScreenLogger.Instance.enabled = false;
            ScreenLogger.Instance.Clear();
        }

#if JX3M && UNITY_EDITOR
        LuaLoader.Instance.UseCache = false;
#endif
    }

    StringBuilder m_strBuilder = new StringBuilder(256);

    public void LogLua(string path, int sizeInBytes, bool loadFromCache)
    {
        m_strBuilder.Length = 0;
        m_strBuilder.AppendFormat("{0:0.00} ({1:0.00}kb) {2} {3}", Time.time, (double)sizeInBytes / 1024.0f, loadFromCache ? "#LuaCache" : "#LuaIO", path);
        ScreenLogger.Instance.EnqueueDirectly(m_strBuilder.ToString(), LogType.Log);
    }

    public void LogSync(string path, double duration)
    {
        m_strBuilder.Length = 0;
        m_strBuilder.AppendFormat("{0:0.00} ({1:0.00}ms) #Sync {2}", Time.time, duration, path);
        ScreenLogger.Instance.EnqueueDirectly(m_strBuilder.ToString(), LogType.Log);
        ScreenLogger.Instance.SyncTopN.TryAdd(duration, path);
    }

    public void LogAsync(string path, double duration)
    {
        m_strBuilder.Length = 0;
        m_strBuilder.AppendFormat("{0:0.00} ({1:0.00}ms) #Async {2}", Time.time, duration, path);
        ScreenLogger.Instance.EnqueueDirectly(m_strBuilder.ToString(), LogType.Warning);
        ScreenLogger.Instance.AsyncTopN.TryAdd(duration, path);
    }
}
