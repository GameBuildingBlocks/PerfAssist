using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class FooItem
{
    public int SeqID = -1;
    public string Name = "Foo";
    public int Count_A = 0;
    public float Time_A = 0.0f;
    public int Count_B = 0;
    public float Time_B = 0.0f;

    public static FooItem MakeRandom()
    {
        return new FooItem()
        {
            SeqID = (int)(Random.value * 100.0f),
            Name = "Foo " + PAEditorUtil.GetRandomString(),
            Count_A = (int)(Random.value * 100.0f),
            Time_A = (Random.value * 100.0f),
            Count_B = (int)(Random.value * 100.0f),
            Time_B = (Random.value * 100.0f),
        };
    }
}

public class TableViewDemoWindow : EditorWindow
{
    [MenuItem(PAEditorConst.DemoTestPath + "/TableView Demo")]
    static void Create()
    {
        TableViewDemoWindow w = EditorWindow.GetWindow<TableViewDemoWindow>();
        if (w.GetType().Name == "DemoWindow")
        {
            w.minSize = new Vector2(800, 600);
            w.Show();
        }
    }

    void Awake()
    {
        // create the table with a specified object type
        _table = new TableView(this, typeof(FooItem));
        
        // setup the description for content
        _table.AddColumn("Name", "Name", 0.5f, TextAnchor.MiddleLeft);
        _table.AddColumn("Count_A", "Count_A", 0.1f);
        _table.AddColumn("Time_A", "Time_A", 0.15f, TextAnchor.MiddleCenter, "0.000");
        _table.AddColumn("Count_B", "Count_B", 0.1f);
        _table.AddColumn("Time_B", "Time_B", 0.15f, TextAnchor.MiddleCenter, "0.0");

        // add test data
        List<object> entries = new List<object>();
        for (int i = 0; i < 100; i++)
            entries.Add(FooItem.MakeRandom());
        _table.RefreshData(entries);

        // register the event-handling function
        _table.OnSelected += TableView_Selected;
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginArea(new Rect(20, 20, position.width * 0.8f, position.height - 80));
        if (_table != null)
            _table.Draw(new Rect(0, 0, position.width * 0.8f, position.height - 80));
        GUILayout.EndArea();

        GUILayout.EndVertical();
    }

    void TableView_Selected(object selected, int col)
    {
        FooItem foo = selected as FooItem;
        if (foo == null)
        {
            Debug.LogErrorFormat("the selected object is not a valid one. ({0} expected, {1} got)",
                typeof(FooItem).ToString(), selected.GetType().ToString());
            return;
        }

        string text = string.Format("object '{0}' selected. (col={1})", foo.Name, col);
        Debug.Log(text);
        ShowNotification(new GUIContent(text));
    }

    void OnDestroy()
    {
        if (_table != null)
            _table.Dispose();

        _table = null;
    }

    TableView _table;
}
