using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class LogUtil
{
    public static string CombinePaths(params string[] paths)
    {
        if (paths == null)
        {
            throw new ArgumentNullException("paths");
        }

        return paths.Aggregate(Path.Combine);
    }

    public static string FormatDateAsFileNameString(DateTime dt)
    {
        return string.Format("{0:0000}-{1:00}-{2:00}", dt.Year, dt.Month, dt.Day);
    }

    public static string FormatTimeAsFileNameString(DateTime dt)
    {
        return string.Format("{0:00}-{1:00}-{2:00}", dt.Hour, dt.Minute, dt.Second);
    }

    public static bool EnableInMemoryStorage = false;
    public static int InMemoryItemMaxCount = 3;

    public static List<string> InMemoryExceptions = new List<string>();
    public static List<string> InMemoryErrors = new List<string>();
    public static void PushInMemoryException(string exception)
    {
        InMemoryExceptions.Add(exception);

        while (InMemoryExceptions.Count > InMemoryItemMaxCount)
        {
            InMemoryExceptions.RemoveAt(0);
        }
    }
    public static void PushInMemoryError(string error)
    {
        InMemoryErrors.Add(error);

        while (InMemoryErrors.Count > InMemoryItemMaxCount)
        {
            InMemoryErrors.RemoveAt(0);
        }
    }
}
