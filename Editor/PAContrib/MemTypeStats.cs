using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

public class MemStats
{
    public static void ShowTypeStats(MemType mt)
    {
        if (mt == null)
        {
            Debug.LogError("invalid type, ignored.");
            return;
        }

        if (mt.TypeName.EndsWith("System.String"))  // this would excludes 'System.String[]'
        {
            ShowStringStats(mt);
        }

        // accumulate by 'referenced by' types
        Dictionary<string, HashSet<MemObject>> referenceMap = new Dictionary<string, HashSet<MemObject>>();
        foreach (var obj in mt.Objects)
        {
            MemObject mo = obj as MemObject;
            if (mo != null && mo._thing != null)
            {
                foreach (var referencer in mo._thing.referencedBy)
                {
                    string referencerTypeName = MemUtil.GetGroupName(referencer);
                    HashSet<MemObject> things;
                    if (!referenceMap.TryGetValue(referencerTypeName, out things))
                    {
                        things = new HashSet<MemObject>();
                        referenceMap[referencerTypeName] = things;
                    }
                    things.Add(mo);
                }
            }
        }

        List<KeyValuePair<int, string>> lines = new List<KeyValuePair<int, string>>();
        foreach (var p in referenceMap)
        {
            HashSet<MemObject> objects = p.Value;

            int totalSize = 0;
            foreach (var obj in objects)
                totalSize += obj.Size;
            lines.Add(new KeyValuePair<int, string>(objects.Count, string.Format("<{0, 80}> {1, 10}, {2, 10}", p.Key, objects.Count, EditorUtility.FormatBytes(totalSize))));
        }
        lines.Sort((x, y) => x.Key.CompareTo(y.Key) * -1); // would sort all results from the largest to the smallest

        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("----- <type: {0}> -----\n", mt.TypeName);
        sb.AppendFormat(" {0} objects ({1}) are referenced by {2} types listed below:\n", mt.Objects.Count, mt.SizeLiterally, lines.Count);
        sb.AppendFormat("-----------------------\n");
        sb.AppendFormat("<{0, 80}> {1, 10}, {2, 10}\n", "type", "count", "size");
        sb.AppendFormat("<{0, 80}> {1, 10}, {2, 10}\n", "----", "-----", "----");
        foreach (var line in lines)
            sb.AppendLine(line.Value);
        UnityEngine.Debug.Log(sb.ToString());
    }

    public static void ShowStringStats(MemType mt)
    {
        Dictionary<string, int> counter = new Dictionary<string, int>();

        int pathCount = 0;
        int winPathCount = 0;
        StringBuilder sb = new StringBuilder();
        foreach (var obj in mt.Objects)
        {
            MemObject mo = obj as MemObject;
            if (mo != null)
            {
                if (mo.InstanceName.Split(new char[] { '/' }).Length >= 3)
                {
                    pathCount++;
                }
                if (mo.InstanceName.Split(new char[] { '\\' }).Length >= 3)
                {
                    sb.AppendFormat("  {0}\n", mo.InstanceName);
                    winPathCount++;
                }

                if (counter.ContainsKey(mo.InstanceName))
                {
                    counter[mo.InstanceName]++;
                }
                else
                {
                    counter.Add(mo.InstanceName, 1);
                }
            }
        }

        UnityEngine.Debug.LogFormat("path: {0}, winPath: {1}", pathCount, winPathCount);
        UnityEngine.Debug.LogFormat("all win paths: \n{0}", sb.ToString());

        List<KeyValuePair<int, string>> lines = new List<KeyValuePair<int, string>>();
        foreach (var p in counter)
        {
            if (p.Value >= 2)
            {
                lines.Add(new KeyValuePair<int, string>(p.Value, p.Key));
            }
        }
        lines.Sort((x, y) => x.Key.CompareTo(y.Key) * -1); // would sort all results from the largest to the smallest
        if (lines.Count > 100)
        {
            lines.RemoveRange(100, lines.Count - 100);
        }

        UnityEngine.Debug.Log("----- string repetitions -----");
        foreach (var line in lines)
        {
            try
            {
                UnityEngine.Debug.LogFormat(" {0, 5} {1}\n", line.Key, line.Value);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogFormat(" {0, 5} {1}: {2}\n", line.Key, "<invalid string>", ex.Message);
            }
        }
    }
}
