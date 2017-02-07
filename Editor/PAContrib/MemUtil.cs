using MemoryProfilerWindow;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEditorInternal;

public class MemUtil 
{
    private static Regex _ipReg = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
    public static bool ValidateIPString(string ip)
    {
        return !string.IsNullOrEmpty(ip) && _ipReg.IsMatch(ip);
    }

    public static bool IsLocalhostIP(string ip)
    {
        return ip == MemConst.LocalhostIP;
    }

    public const int PLAYER_DIRECT_IP_CONNECT_GUID = 65261;
    public static bool IsProfilerConnectedRemotely { get { return ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID; } }

    public static void NotifyError(string format, params object[] args)
    {
        string content = string.Format(format, args);
        Debug.LogError(content);
        EditorWindow.focusedWindow.ShowNotification(new GUIContent(content));
    }

    public static string SnapshotsDir = string.Format("{0}/mem_snapshots", Application.persistentDataPath);

    public static string GetFullpath(string filename)
    {
        return string.IsNullOrEmpty(filename) ? "" : string.Format("{0}/{1}", SnapshotsDir, filename);
    }

    public static string[] GetFiles()
    {
        try
        {
            if (!Directory.Exists(SnapshotsDir))
            {
                Directory.CreateDirectory(SnapshotsDir);
            }

            string[] files = Directory.GetFiles(SnapshotsDir);
            for (int i = 0; i < files.Length; i++)
            {
                int begin = files[i].LastIndexOfAny(new char[] { '\\', '/' });
                if (begin != -1)
                {
                    files[i] = files[i].Substring(begin + 1);
                }
            }
            return files;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return new string[] { };
        }

    }

    public static PackedMemorySnapshot Load(string filename)
    {
        try
        {
            if (string.IsNullOrEmpty(filename))
                throw new Exception("bad_load: filename is empty.");

            string fullpath = GetFullpath(filename);
            if (!File.Exists(fullpath))
                throw new Exception(string.Format("bad_load: file not found. ({0})", fullpath));

            using (Stream stream = File.Open(fullpath, FileMode.Open))
            {
                return new BinaryFormatter().Deserialize(stream) as PackedMemorySnapshot;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogErrorFormat("bad_load: exception occurs while loading '{0}'.", filename);
            Debug.LogException(ex);
            return null;
        }
    }

    public static string Save(PackedMemorySnapshot snapshot)
    {
        try
        {
            string filename = GetFullpath(string.Format("{0}-{1}.memsnap",
                SysUtil.FormatDateAsFileNameString(DateTime.Now),
                SysUtil.FormatTimeAsFileNameString(DateTime.Now)));

            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (Stream stream = File.Open(filename, FileMode.Create))
            {
                bf.Serialize(stream, snapshot);
            }
            return filename;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return "";
        }
    }

    public static string GetGroupName(ThingInMemory thing)
    {
        if (thing is NativeUnityEngineObject)
            return (thing as NativeUnityEngineObject).className ?? "MissingName";
        if (thing is ManagedObject)
            return (thing as ManagedObject).typeDescription.name;
        return thing.GetType().Name;
    }
    public static int GetCategory(ThingInMemory thing)
    {
        if (thing is NativeUnityEngineObject)
            return 1;
        if (thing is ManagedObject)
            return 2;
        if (thing is GCHandle)
            return 3;
        if (thing is StaticFields)
            return 4;

        return 5;
    }

    public static string GetCategoryLiteral(ThingInMemory thing)
    {
        if (thing is NativeUnityEngineObject)
            return "[native] ";
        if (thing is ManagedObject)
            return "[managed] ";
        if (thing is GCHandle)
            return "[gchandle] ";
        if (thing is StaticFields)
            return "[static] ";

        return "[unknown] ";
    }

    public static bool MatchSizeLimit(int size, int curLimitIndex)
    {
        if (curLimitIndex == 0)
            return true;

        switch (curLimitIndex)
        {
            case 0:
                return true;

            case 1:
                return size >= MemConst._1MB;

            case 2:
                return size >= MemConst._1KB && size < MemConst._1MB;

            case 3:
                return size < MemConst._1KB;

            default:
                return false;
        }
    }

    public static void LoadSnapshotProgress(float progress, string tag)
    {
        EditorUtility.DisplayProgressBar("Loading in progress, please wait...", string.Format("{0} - {1}%", tag, (int)(progress * 100.0f)), progress);

        if (progress >= 1.0f)
            EditorUtility.ClearProgressBar();
    }

    public static string IntStrWithSign(int val) { return val.ToString("+#;-#;0"); }
    public static string GetSign(int val) { return val >= 0 ? "+" : "-"; }
}
