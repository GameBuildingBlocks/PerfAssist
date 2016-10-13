using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class TableViewTestWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 600.0f;

    Vector2 _scrollPos = Vector2.zero;

    [MenuItem("Window/TableViewTest")]
    static void Create()
    {
        TableViewTestWindow w = EditorWindow.GetWindow<TableViewTestWindow>();
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

            List<CoTableEntry> entries = new List<CoTableEntry>();
            for (int i = 0; i < 100; i++)
                entries.Add(new CoTableEntry());

            Panel_CoTable.Instance.RefreshEntries(entries);
            Panel_CoTable.Instance.OnCoroutineSelected += w.TablePanel_CoroutineSelected;
        }
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
        //GUILayout.BeginHorizontal();
        //_enableTracking = GUILayout.Toggle(_enableTracking, "EnableTracking", GUILayout.Height(ToolbarHeight));
        //GUILayout.EndHorizontal();

        GUILayout.BeginVertical();

        GUILayout.BeginArea(new Rect(0, ToolbarHeight, DataTableWidth, position.height - ToolbarHeight));
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            Panel_CoTable.Instance.DrawTable();
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();

        GUILayout.EndVertical();
    }

    void TablePanel_CoroutineSelected(int coSeqID)
    {
        Debug.LogFormat("line selected: {0}", coSeqID);
    }
}
