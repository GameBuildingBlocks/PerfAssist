using UnityEngine;
using UnityEditor;
using System.Collections;

public delegate bool GOGraphFilter(GameObject go);
public delegate void GOGraphHandler(GameObject go);

public static class GOGraphUtil
{
    public static bool OutputStatistics = true;

    public class GOGraphStats
    {
        public int TotalTouched = 0;
        public int TotalFiltered = 0;
        public int TotalProcessed = 0;
    }

    public static void Foreach(string rootName, GOGraphHandler handler, GOGraphFilter filter = null)
    {
        GameObject go = GameObject.Find(rootName);
        if (go == null)
        {
            Debug.LogErrorFormat("[GOGraph] root '{0}' not found.", rootName);
            return;
        }

        GOGraphStats stats = OutputStatistics ? new GOGraphStats() : null;

        ForeachDescendantRecursively(go, handler, filter, stats);

        if (OutputStatistics)
        {
            Debug.LogFormat("[GOGraph] Stats - Total: {0}, Filtered: {1}, Processed: {2}",
                stats.TotalTouched,
                stats.TotalFiltered,
                stats.TotalProcessed);
        }
    }

    public static void ForeachDescendantRecursively(GameObject go, GOGraphHandler handler, GOGraphFilter filter, GOGraphStats stats)
    {
        if (go == null)
            return;

        if (stats != null)
            stats.TotalTouched++;

        if (filter != null && filter(go))
        {
            if (stats != null)
                stats.TotalFiltered++;
            return;
        }

        if (handler != null)
        {
            handler(go);

            if (stats != null)
                stats.TotalProcessed++;
        }

        foreach (Transform ct in go.transform)
        {
            ForeachDescendantRecursively(ct.gameObject, handler, filter, stats);
        }
    }
}
