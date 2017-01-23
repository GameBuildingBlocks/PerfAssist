using UnityEngine;
using System.Collections;
using UnityEditorInternal;
using System;
using UnityEditor;

public static class TrackerModeConsts
{
    public static string[] Modes = new string[] { "Editor", "Remote", "Daemon", "File" };
    public static string[] ModesDesc = new string[]
    {
        "Mode 'Editor': connects to the local in-editor game, and ONLY support 'Native' memory object type.",
        "Mode 'Remote': connects to the remote ip, and support all types if il2cpp is enabled.",
        "Mode 'Daemon': opens a server for a remote device to connect in, and request for snapshots on-demand.",
        "Mode 'File': opens a saved session from local file system."
    };
}

public static class TrackerModeUtil
{
    public static bool Handle_ServerLogging(eNetCmd cmd, UsCmd c)
    {
        UsLogPacket pkt = new UsLogPacket(c);

        string logTypeStr = "";
        switch (pkt.LogType)
        {
            case UsLogType.Error:
            case UsLogType.Exception:
            case UsLogType.Assert:
            case UsLogType.Warning:
                logTypeStr = string.Format("{1}", pkt.LogType);
                break;

            case UsLogType.Log:
            default:
                break;
        }

        string timeStr = string.Format("{0:0.00}({1})", pkt.RealtimeSinceStartup, pkt.SeqID);

        string ret = string.Format("{0} {1} <color=white>{2}</color>", timeStr, logTypeStr, pkt.Content);

        if (!string.IsNullOrEmpty(pkt.Callstack))
        {
            ret += string.Format("\n<color=gray>{0}</color>", pkt.Callstack);
        }

        Debug.Log(ret);

        return true;
    }

    public static bool SaveBin(string filePath, object obj)
    {
        return true;
    }

    public static bool SaveJson(string filePath, object obj)
    {
        return true;
    }

    public static void Connect(string ip)
    {
        if (NetManager.Instance == null)
            return;

        ProfilerDriver.connectedProfiler = -1;
        if (NetManager.Instance.IsConnected)
            NetManager.Instance.Disconnect();

        try
        {
            if (!MemUtil.ValidateIPString(ip))
                throw new Exception("Invaild IP");

            if (!NetManager.Instance.Connect(ip))
                throw new Exception("Bad Connect");

            if (!MemUtil.IsLocalhostIP(ip))
            {
                ProfilerDriver.DirectIPConnect(ip);
                if (!MemUtil.IsProfilerConnectedRemotely)
                    throw new Exception("Bad Connect");
            }

            EditorPrefs.SetString("ResourceTrackerLastConnectedIP", ip);
        }
        catch (Exception ex)
        {
            EditorWindow.focusedWindow.ShowNotification(new GUIContent(string.Format("Connecting '{0}' failed: {1}", ip, ex.Message)));
            Debug.LogException(ex);

            ProfilerDriver.connectedProfiler = -1;
            if (NetManager.Instance.IsConnected)
                NetManager.Instance.Disconnect();
        }
    }
}
