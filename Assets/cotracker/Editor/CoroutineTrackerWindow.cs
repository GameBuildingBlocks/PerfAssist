using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class CoroutineTrackerWindow : EditorWindow
{
    public static float ToolbarHeight = 30.0f;
    public static float DataTableWidth = 600.0f;
    public static float CoroutineInfoAreaHeight = 300.0f;

    // bound variables
    //bool _enableTracking = true;
    Vector2 _scrollPositionLeft;
    Vector2 _scrollRightUpper;
    Vector2 _scrollRightLower;

    string _coroutineName = "<coroutine name>";
    string _coroutineInfo = "<coroutine info>";
    string _coroutineStacktrace = "<coroutine creation stacktrace>";
    string _coroutineExecutions = "<coroutine executions>";

    float _selectedSnapshotTime = 0.0f;

    CoroutineEditorDatabase _database;

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

    void GraphPanel_SelectionChanged(int selectionIndex)
    {
        _selectedSnapshotTime = CoGraphUtil.GetSnapshotTime(selectionIndex);
        List<CoTableEntry> entries = _database.PopulateEntries(_selectedSnapshotTime);
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
        if (Event.current.commandName == "AppStarted")
        {
            _database = new CoroutineEditorDatabase();
            RuntimeCoroutineStats.Instance.OnBroadcast += _database.Receive;
            Panel_CoGraph.Instance.SelectionChanged += GraphPanel_SelectionChanged;
            Panel_CoTable.Instance.OnCoroutineSelected += TablePanel_CoroutineSelected;
        }

        //GUILayout.BeginHorizontal();
        //_enableTracking = GUILayout.Toggle(_enableTracking, "EnableTracking", GUILayout.Height(ToolbarHeight));
        //GUILayout.EndHorizontal();

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
            GUILayout.BeginVertical();
            Rect r_upper = new Rect(position.width - DataTableWidth, ToolbarHeight, DataTableWidth, position.height - ToolbarHeight - CoroutineInfoAreaHeight);
            Rect r_lower = new Rect(position.width - DataTableWidth, position.height - CoroutineInfoAreaHeight, DataTableWidth, CoroutineInfoAreaHeight);

            GUILayout.BeginArea(r_upper);
            {
                _scrollRightUpper = GUILayout.BeginScrollView(_scrollRightUpper, GUIStyle.none, GUI.skin.verticalScrollbar);
                Panel_CoTable.Instance.DrawTable();
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
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

    void TablePanel_CoroutineSelected(int coSeqID)
    {
        CoroutineInfo info = _database.GetCoroutineInfo(coSeqID);
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
            if (item.Key <= _selectedSnapshotTime && item.Key > _selectedSnapshotTime - CoroutineEditorDatabase.SnapshotInterval)
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
