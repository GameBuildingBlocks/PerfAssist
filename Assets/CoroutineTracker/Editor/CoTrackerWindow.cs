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
}

public class CoTrackerWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 600.0f;
    public static float CoroutineInfoAreaHeight = 300.0f;

    // bound variables
    //bool _enableTracking = true;
    Vector2 _scrollPositionLeft;
    Vector2 _scrollRightLower;

    string _coroutineName = "<coroutine name>";
    string _coroutineInfo = "<coroutine info>";
    string _coroutineStacktrace = "<coroutine creation stacktrace>";
    string _coroutineExecutions = "<coroutine executions>";

    float _selectedSnapshotTime = 0.0f;

    CoTrackerDatabase _database;

    TableView _table;

    [MenuItem(PAEditorConst.MenuPath + "/CoroutineTracker")]
    static void Create()
    {
        CoTrackerWindow w = EditorWindow.GetWindow<CoTrackerWindow>();
        w.minSize = new Vector2(1280, 720);
        w.Show();
    }

    void GraphPanel_SelectionChanged(int selectionIndex)
    {
        if (_table != null)
        {
            _selectedSnapshotTime = CoGraphUtil.GetSnapshotTime(selectionIndex);
            List<object> entries = _database.PopulateEntries(_selectedSnapshotTime);
            _table.RefreshData(entries);
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
        if (Event.current.type == EventType.ExecuteCommand)
        {
            switch (Event.current.commandName)
            {
                case "AppStarted":
                    {
                        GraphIt.Instance = new GraphIt();

                        _database = new CoTrackerDatabase();
                        RuntimeCoroutineStats.Instance.OnBroadcast += _database.Receive;
                        CoTrackerPanel_Graph.Instance.SelectionChanged += GraphPanel_SelectionChanged;

                        _table = new TableView(this, typeof(CoTableEntry));

                        // setup the description for content
                        _table.AddColumn("Name", "Name", 0.58f, TextAnchor.MiddleLeft);
                        _table.AddColumn("ExecSelectedCount", "Cnt", 0.06f, TextAnchor.MiddleCenter);
                        _table.AddColumn("ExecSelectedTime", "Time", 0.1f, TextAnchor.MiddleCenter, "0.000");
                        _table.AddColumn("ExecAccumCount", "Cnt_Sum", 0.12f, TextAnchor.MiddleCenter);
                        _table.AddColumn("ExecAccumTime", "Time_Sum", 0.14f, TextAnchor.MiddleCenter, "0.000");

                        // register the event-handling function
                        _table.OnSelected += TablePanel_CoroutineSelected;
                    }
                    break;

                case "AppDestroyed":
                    {
                        if (_table != null)
                        {
                            _table.Dispose();
                            _table = null;
                        }
                    }
                    break;

                default:
                    break;
            }
        }
        else
        {
            //GUILayout.BeginHorizontal();
            //_enableTracking = GUILayout.Toggle(_enableTracking, "EnableTracking", GUILayout.Height(ToolbarHeight));
            //GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                Rect r = new Rect(0, ToolbarHeight, position.width - DataTableWidth, position.height - ToolbarHeight);
                GUILayout.BeginArea(r);
                {
                    _scrollPositionLeft = GUILayout.BeginScrollView(_scrollPositionLeft, GUIStyle.none, GUI.skin.verticalScrollbar);
                    CoTrackerPanel_Graph.Instance.DrawGraphs(r);
                    GUILayout.EndScrollView();
                }
                GUILayout.EndArea();
            }
            {
                GUILayout.BeginVertical();
                Rect r_upper = new Rect(position.width - DataTableWidth, ToolbarHeight, DataTableWidth, position.height - ToolbarHeight - CoroutineInfoAreaHeight);
                Rect r_lower = new Rect(position.width - DataTableWidth, position.height - CoroutineInfoAreaHeight, DataTableWidth, CoroutineInfoAreaHeight);

                if (_table != null)
                    _table.Draw(r_upper);

                GUILayout.BeginArea(r_lower);
                {
                    _scrollRightLower = GUILayout.BeginScrollView(_scrollRightLower, GUIStyle.none, GUI.skin.verticalScrollbar);
                    GUILayout.Label(_coroutineName);
                    GUILayout.Label(_coroutineInfo);
                    GUILayout.TextArea(_coroutineStacktrace);
                    GUILayout.TextArea(_coroutineExecutions);
                    GUILayout.EndScrollView();
                }
                GUILayout.EndArea();

                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }
    }

    void TablePanel_CoroutineSelected(object selected, int col)
    {
        CoTableEntry entry = selected as CoTableEntry;
        if (entry == null)
            return;

        CoroutineInfo info = _database.GetCoroutineInfo(entry.SeqID);
        if (info == null)
            return;

        string termTime = info.termination != null ? info.termination.timestamp.ToString("0.000") : "(not yet)";

        _coroutineName = string.Format("{0}", info.creation.mangledName);
        _coroutineInfo = string.Format("SeqID: {0}, Created: {1:0.00}, Terminated: {2}", info.creation.seqID, info.creation.timestamp, termTime);
        _coroutineStacktrace = "Stacktrace:\n\n" + info.creation.stacktrace;

        StringBuilder sb = new StringBuilder(1024);
        sb.AppendFormat("Executions: ({0})\n\n", info.executions.Count);
        for (int i = 0; i < info.executions.Count; i++)
        {
            string sel = "";
            var item = info.executions[i];
            if (item.Key <= _selectedSnapshotTime && item.Key > _selectedSnapshotTime - CoTrackerDatabase.SnapshotInterval)
            {
                sel = "(in selected snapshot)";
            }

            sb.AppendFormat("  ({0}) timestamp: {1:0.000} duration: {2:0.000} {3}\n", i, item.Key, item.Value, sel);

            if (sb.Length > 10000)
            {
                sb.Append("(cutting the rest off since it's too long...)");
                break;
            }
        }
        _coroutineExecutions = sb.ToString();
    }
}
