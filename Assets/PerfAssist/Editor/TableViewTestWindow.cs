using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CoTableEntry
{
    public int SeqID = -1;
    public string Name = "Foo";
    public int ExecSelectedCount = 0;
    public float ExecSelectedTime = 0.0f;
    public int ExecAccumCount = 0;
    public float ExecAccumTime = 0.0f;

    public static CoTableEntry MakeRandom()
    {
        return new CoTableEntry()
        {
            SeqID = (int)(Random.value * 100.0f),
            Name = "Foo " + (Random.value * 100.0f).ToString(),
            ExecSelectedCount = (int)(Random.value * 100.0f),
            ExecSelectedTime = (Random.value * 100.0f),
            ExecAccumCount = (int)(Random.value * 100.0f),
            ExecAccumTime = (Random.value * 100.0f),
        };
    }
}


public class TableViewTestWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 600.0f;

    TableView _table;

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

        _table = new TableView(this, typeof(CoTableEntry));

        _table.AddColumn("Name", "Name", 0.58f, TextAnchor.MiddleLeft);
        _table.AddColumn("ExecSelectedCount", "Cnt", 0.06f);
        _table.AddColumn("ExecSelectedTime", "Time", 0.1f, TextAnchor.MiddleCenter, "0.000");
        _table.AddColumn("ExecAccumCount", "Cnt_Sum", 0.12f);
        _table.AddColumn("ExecAccumTime", "Time_Sum", 0.14f, TextAnchor.MiddleCenter, "0.000");

        List<object> entries = new List<object>();
        for (int i = 0; i < 100; i++)
            entries.Add(CoTableEntry.MakeRandom());
        _table.RefreshData(entries);

        _table.OnSelected += TableView_Selected;
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

        if (_table != null)
            _table.Draw(new Rect(0, ToolbarHeight, position.width * 0.6f, position.height - ToolbarHeight));

        GUILayout.EndVertical();
    }

    void TableView_Selected(object selected, int col)
    {
        Debug.LogFormat("line selected: {0}, {1}", ((CoTableEntry)(selected)).SeqID, col);
    }
}
