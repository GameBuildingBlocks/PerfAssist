using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;

public class CoroutineTrackerWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 500.0f;

    // bound variables
    bool _enableTracking = true;
    Vector2 _scrollPositionLeft;
    Vector2 _scrollPositionRight;

    Rect _lastPos;

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
                w._lastPos = rect;

                DataTableWidth = rect.width * 0.5f;
            }
        }
    }

    void GraphPanel_SelectionChanged(int selectionIndex)
    {
        float time = CoGraphUtil.GetSnapshotTime(selectionIndex);
        List<CoTableEntry> entries = CoroutineEditorDatabase.Instance.PopulateEntries(time);
        Panel_CoTable.Instance.RefreshEntries(entries);
    }

    void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    void OnGUI()
    {
        if (!Mathf.Approximately(position.width, _lastPos.width) || !Mathf.Approximately(position.height, _lastPos.height))
        {
            // resized.

            DataTableWidth = position.width * 0.5f;
            _lastPos = position;
        }

        if (Event.current.commandName == "AppStarted")
        {
            CoroutineStatisticsV2.Instance.OnBroadcast += CoroutineEditorDatabase.Instance.Receive;
            Panel_CoGraph.Instance.SelectionChanged += GraphPanel_SelectionChanged;

            DataTableWidth = position.width * 0.5f;
            _lastPos = position;
        }

        GUILayout.BeginHorizontal();
        _enableTracking = GUILayout.Toggle(_enableTracking, "EnableTracking", GUILayout.Height(ToolbarHeight));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        {
            Rect r = new Rect(0, ToolbarHeight, position.width - DataTableWidth, position.height - ToolbarHeight);
            GUILayout.BeginArea(r);
            {
                _scrollPositionLeft = GUILayout.BeginScrollView(_scrollPositionLeft, GUIStyle.none, GUI.skin.verticalScrollbar);
                Panel_CoGraph.Instance.DrawGraphs(r);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
        {
            Rect r1 = new Rect(position.width - DataTableWidth, ToolbarHeight, DataTableWidth, position.height - ToolbarHeight);
            GUILayout.BeginArea(r1);
            {
                _scrollPositionRight = GUILayout.BeginScrollView(_scrollPositionRight, GUIStyle.none, GUI.skin.verticalScrollbar);
                Panel_CoTable.Instance.DrawTable();
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
        GUILayout.EndHorizontal();
    }
}
