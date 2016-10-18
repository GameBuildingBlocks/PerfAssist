using UnityEngine;
using System.Collections;
using UnityEditor;

public class PAEditorUtil
{
    public static void DrawLabel(string content, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(content, style, GUILayout.Width(style.CalcSize(new GUIContent(content)).x + 3));
        EditorGUILayout.EndHorizontal();
    }
}
