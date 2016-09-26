using UnityEngine;
using System.Collections;
using UnityEditor;

public class CoroutineTrackerWindow : EditorWindow
{
    static float s_tableWidth = 150.0f;

    bool _enableTracking = true;

    Vector2 _scrollPositionLeft;
    Vector2 _scrollPositionRight;

    [MenuItem("Window/CoroutineTracker")]
    static void Create()
    {
        CoroutineTrackerWindow w = EditorWindow.GetWindow<CoroutineTrackerWindow>();
        w.Show();

        if (w != null)
        {
            var rect = w.position;
            rect.width = Mathf.Max(1280, rect.width);
            rect.height = Mathf.Max(720, rect.height);
            if (!Mathf.Approximately(rect.width, w.position.width) || 
                !Mathf.Approximately(rect.height, w.position.height))
            {
                w.position = rect;
            }
        }
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        _enableTracking = GUILayout.Toggle(_enableTracking, "EnableTracking", GUILayout.MaxWidth(150), GUILayout.Height(50));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            GUILayout.BeginArea(new Rect(0, 50, position.width - s_tableWidth, position.height - 50));
            _scrollPositionLeft = GUILayout.BeginScrollView(_scrollPositionLeft, GUIStyle.none, GUI.skin.verticalScrollbar);
            GUILayout.Label("<to be added>");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        {
            GUILayout.BeginArea(new Rect(position.width - s_tableWidth, 50, s_tableWidth, position.height - 50));
            _scrollPositionRight = GUILayout.BeginScrollView(_scrollPositionRight, GUIStyle.none, GUI.skin.verticalScrollbar);
            GUILayout.Label("<to be added>");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
        GUILayout.EndHorizontal();
    }
}
