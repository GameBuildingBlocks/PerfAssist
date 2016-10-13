using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class TableViewTestWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 600.0f;

    TableView _table;
    Vector2 _scrollPos = Vector2.zero;

    [MenuItem("Window/TableViewTest")]
    static void Create()
    {
        TableViewTestWindow w = EditorWindow.GetWindow<TableViewTestWindow>();
        if (w.GetType().Name == "TableViewTestWindow")
        {
            w.Show();
        }
    }

    void Awake()
    {
        var rect = position;
        rect.width = Mathf.Max(1280, rect.width);
        rect.height = Mathf.Max(720, rect.height);
        if (!Mathf.Approximately(rect.width, position.width) ||
            !Mathf.Approximately(rect.height, position.height))
        {
            position = rect;
        }

        _table = new TableView(this);

        _table.AddColumn("Name", "Name", 0.58f);
        _table.AddColumn("ExecSelectedCount", "Cnt", 0.06f);
        _table.AddColumn("ExecSelectedTime", "Time", 0.1f);
        _table.AddColumn("ExecAccumCount", "Cnt_Sum", 0.12f);
        _table.AddColumn("ExecAccumTime", "Time_Sum", 0.14f);

        List<CoTableEntry> entries = new List<CoTableEntry>();
        for (int i = 0; i < 100; i++)
            entries.Add(new CoTableEntry());
        _table.RefreshEntries(entries);

        _table.OnLineSelected += TableView_LineSelected;
    }

    void OnDestroy()
    {
        if (_table != null)
            _table.Dispose();

        _table = null;
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
            if (_table != null)
                _table.DrawTable();
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();

        GUILayout.EndVertical();
    }

    void TableView_LineSelected(int coSeqID)
    {
        Debug.LogFormat("line selected: {0}", coSeqID);
    }
}
