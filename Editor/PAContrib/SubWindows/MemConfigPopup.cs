using UnityEngine;
using System.Collections;
using UnityEditor;

public class MemConfigPopup : PopupWindowContent
{
    public bool AutoSaveOnSnapshot { get { return _autoSaveOnSnapshot; } }
    bool _autoSaveOnSnapshot = true;

    public bool HideIdenticalInDiff { get { return _hideIdenticalInDiff; } }
    bool _hideIdenticalInDiff = true;

    public MemConfigPopup()
    {
        _autoSaveOnSnapshot = EditorPrefs.GetBool(MemPrefs.AutoSaveOnSnapshot);
        _hideIdenticalInDiff = EditorPrefs.GetBool(MemPrefs.HideIdenticalInDiff);
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(200, 80);
    }

    public override void OnGUI(Rect rect)
    {
        EditorGUILayout.BeginVertical();
        GUILayout.Space(5);
        ShowConfigBool(ref _autoSaveOnSnapshot, "Auto-Save on snapshot", MemPrefs.AutoSaveOnSnapshot);
        ShowConfigBool(ref _hideIdenticalInDiff, "Hide Identical In Diff", MemPrefs.HideIdenticalInDiff);
        EditorGUILayout.EndVertical();
    }

    void ShowConfigBool(ref bool configVar, string text, string prefText)
    {
        bool newVal = EditorGUILayout.Toggle(text, configVar);
        if (configVar != newVal)
        {
            configVar = newVal;
            EditorPrefs.SetBool(prefText, configVar);
        }
    }
}

