using UnityEngine;
using System.Collections;

public class MemConst 
{
    public static float TopBarHeight = 25;
    public static int InspectorWidth = 400;
    public static string[] ShowTypes = new string[] { "Table View", "TreeMap View" };

    public static string[] MemTypeCategories = new string[] { "All", "Native", "Managed", "Others" };
    public static string[] MemTypeLimitations = new string[] { "All", "n >= 1MB", "1MB > n >= 1KB", "n < 1KB" };

    public static int TableBorder = 10;
    public static float SplitterRatio = 0.4f;

    public static int _1KB = 1024;
    public static int _1MB = _1KB * _1KB;

    public static readonly string SearchResultTypeString = "{search_result}";
}

public class MemStyles
{
    public static GUIStyle Toolbar = "Toolbar";
    public static GUIStyle ToolbarButton = "ToolbarButton";
    public static GUIStyle Background = "AnimationCurveEditorBackground";

    public static GUIStyle SearchTextField = "ToolbarSeachTextField";
    public static GUIStyle SearchCancelButton = "ToolbarSeachCancelButton";
}
