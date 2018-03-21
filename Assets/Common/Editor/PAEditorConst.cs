using UnityEngine;
using System.Collections;

public class PAEditorConst
{
    public const int BAD_ID = -1;

    public readonly static Color TitleColor = (Color)new Color32(38, 158, 111, 255);        // basically green
    public readonly static Color TitleColorSelected = (Color)new Color32(19, 80, 60, 255);  // dark green

    public readonly static Color SelectionColor = (Color)new Color32(62, 95, 150, 255);
    public readonly static Color SelectionColorDark = (Color)new Color32(62, 95, 150, 128);
    public readonly static string BytesFormatter = "<fmt_bytes>";

    public const string MenuPath = "Window/PerfAssist";
    public const string DemoTestPath = MenuPath + "/Demos and Tests";
    public const string DevCommandPath = MenuPath + "/Dev Command";
}
