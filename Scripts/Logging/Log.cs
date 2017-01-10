using UnityEngine;
using System;

public enum LogLevel
{
    Error,
    Warning,
    Info,
}

public static class Log
{
    public static LogLevel LogLevel = LogLevel.Info;

    public static void Info(object msg, params object[] args)
    {
        if (LogLevel >= LogLevel.Info)
        {
            Debug.Log(_format(msg, args));
        }
    }

    public static void InfoEx(object msg, UnityEngine.Object context)
    {
        if (LogLevel >= LogLevel.Info)
        {
            Debug.Log(msg, context);
        }
    }

    public static void Warning(object msg, params object[] args)
    {
        if (LogLevel >= LogLevel.Warning)
        {
            Debug.LogWarning(_format(msg, args));
        }
    }

    public static void Error(object msg, params object[] args)
    {
        if (LogLevel >= LogLevel.Error)
        {
            Debug.LogError(_format(msg, args));
        }
    }
    public static void Exception(Exception ex)
    {
        if (LogLevel >= LogLevel.Error)
        {
            Debug.LogException(ex);
        }
    }

    public static void Assert(bool condition)
    {
        if (LogLevel >= LogLevel.Error)
        {
            Assert(condition, string.Empty, true);
        }
    }

    public static void Assert(bool condition, string assertString)
    {
        if (LogLevel >= LogLevel.Error)
        {
            Assert(condition, assertString, false);
        }
    }

    public static void Assert(bool condition, string assertString, bool pauseOnFail)
    {
        if (!condition && LogLevel >= LogLevel.Error)
        {
            Debug.LogError("assert failed! " + assertString);

            if (pauseOnFail)
                Debug.Break();
        }
    }

    private static object _format(object msg, params object[] args)
    {
        string fmt = msg as string;
        if (args.Length == 0 || string.IsNullOrEmpty(fmt))
        {
            return msg;
        }
        else
        {
            return string.Format(fmt, args);
        }
    }
}
