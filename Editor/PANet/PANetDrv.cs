using UnityEngine;
using System.Collections;
using System;

public class PANetDrv : IDisposable
{
    public static PANetDrv Instance = new PANetDrv();

    public PANetDrv()
    {
        NetUtil.LogHandler = Debug.LogFormat;
        NetUtil.LogErrorHandler = Debug.LogErrorFormat;

        NetManager.Instance = new NetManager();
        NetManager.Instance.Client.RegisterCmdHandler(eNetCmd.SV_App_Logging, Handle_ServerLogging);
    }

    private bool Handle_ServerLogging(eNetCmd cmd, UsCmd c)
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

    public void Dispose()
    {
        NetManager.Instance.Dispose();
    }
}
