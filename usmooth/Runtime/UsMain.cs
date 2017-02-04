using System;
using UnityEngine;

public class UsMain : IDisposable
{
    public const int MAX_CONTENT_LEN = 1024;

	private long _currentTimeInMilliseconds = 0;
	private long _tickNetLast = 0;
	private long _tickNetInterval = 200;
    private LogService _logServ;
    private utest _test;

    private bool _inGameGui = false;

    public UsMain(bool LogRemotely, bool LogIntoFile, bool InGameGui) 
    {
		Application.runInBackground = true;

        _logServ = new LogService(LogIntoFile, -1, true);

        _test = new utest();

        if (LogRemotely)
        {
            _logServ.LogTargets += LogTarget_Remotely;
        }

		UsNet.Instance = new UsNet();
        UsMain_NetHandlers.Instance = new UsMain_NetHandlers(UsNet.Instance.CmdExecutor);
        UsvConsole.Instance = new UsvConsole();

        GameUtil.Log("on_level loaded.");
        GameInterface.Instance.Init();

        _inGameGui = InGameGui;
	}

    void LogTarget_Remotely(object sender, LogEventArgs args)
    {
        if (UsNet.Instance != null)
        {
            UsCmd c = new UsCmd();
            c.WriteNetCmd(eNetCmd.SV_App_Logging);
            c.WriteInt16((short)args.SeqID);
            c.WriteInt32((int)args.LogType);
            c.WriteStringStripped(args.Content, MAX_CONTENT_LEN);
            c.WriteFloat(args.Time);
            UsNet.Instance.SendCommand(c);
        }
    }

    public void Update()
    {
		_currentTimeInMilliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

		if (_currentTimeInMilliseconds - _tickNetLast > _tickNetInterval)
		{
			if (UsNet.Instance != null) {
				UsNet.Instance.Update ();
			}

			_tickNetLast = _currentTimeInMilliseconds;
		}
	}

    public void Dispose()
    {
        UsNet.Instance.Dispose();
        _test.Dispose();
        _logServ.Dispose();
    }

    public void OnLevelWasLoaded()
    {
        _test.OnLevelWasLoaded();
    }

    public void OnGUI()
    {
        if (_inGameGui)
            _test.OnGUI();
    }
}
