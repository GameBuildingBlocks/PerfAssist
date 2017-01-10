public delegate void NetLogHandler(string fmt, params object[] args);

public static class NetUtil
{
    public static NetLogHandler LogHandler { get; set; }
    public static NetLogHandler LogErrorHandler { get; set; }

    public static void Log(string fmt, params object[] args)
    {
        if (LogHandler != null)
            LogHandler(fmt, args);
    }

    public static void LogError(string fmt, params object[] args)
    {
        if (LogErrorHandler != null)
            LogErrorHandler(fmt, args);
    }
}

