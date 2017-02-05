using UnityEngine;
using System.Collections;

public class MemConst 
{
    public static float TopBarHeight = 60;
    public static int InspectorWidth = 400;
    public static string[] ShowTypes = new string[] { "Table View", "TreeMap View" };

    public static string[] MemTypeCategories = new string[] { "All", "Native", "Managed", "Others" };
    public static string[] MemTypeLimitations = new string[] { "All", "n >= 1MB", "1MB > n >= 1KB", "n < 1KB" };

    public static int TableBorder = 10;
    public static float SplitterRatio = 0.4f;

    public static int _1KB = 1024;
    public static int _1MB = _1KB * _1KB;

    public static readonly string SearchResultTypeString = "{search_result}";
    public static readonly string RemoteIPDefaultText = "<remote_ip>";

    public static readonly string LocalhostIP = "127.0.0.1";

    public static readonly string UPLOAD_HTTP_URL ="http://10.20.80.59:88/ramPush";
    //"http://jx3ml.rdev.kingsoft.net:88/ramPush";

    public static readonly string DiffMarkText_1st = "Mark As 1st";
    public static readonly string DiffMarkText_2nd = "Mark As 2nd";
}

public class MemPrefs
{
    public static readonly string AutoSaveOnSnapshot = "Mem_AutoSaveOnSnapshot";
    public static readonly string Diff_HideIdentical = "Mem_Diff_HideIdentical";
    public static readonly string Diff_HideRemoved = "Mem_Diff_HideRemoved";
    public static readonly string LastConnectedIP = "Mem_LastConnectedIP";
}

public class MemStyles
{
    public static GUIStyle Toolbar = "Toolbar";
    public static GUIStyle ToolbarButton = "ToolbarButton";
    public static GUIStyle Background = "AnimationCurveEditorBackground";

    public static GUIStyle SearchTextField = "ToolbarSeachTextField";
    public static GUIStyle SearchCancelButton = "ToolbarSeachCancelButton";
}
