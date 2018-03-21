using MemoryProfilerWindow;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;


public class TrackerMode_Base
{
    public CrawledMemorySnapshot Selected { get { return _selected != PAEditorConst.BAD_ID ? GetAt(_selected) : null; } }
    public CrawledMemorySnapshot Diff_1st { get { return _1st != PAEditorConst.BAD_ID ? GetAt(_1st) : null; } }
    public CrawledMemorySnapshot Diff_2nd { get { return _2nd != PAEditorConst.BAD_ID ? GetAt(_2nd) : null; } }

    public bool IsDiffing { get { return _isDiffing; } }

    public void AddSnapshot(MemSnapshotInfo snapshot)
    {
        _snapshots.Add(snapshot);
        _selected = _snapshots.Count - 1;

        RefreshIndices();

        // automatically compare the last two, when new snapshot comes
        if (_snapshots.Count > 1)
        {
            _1st = _selected - 1;
            _2nd = _selected;
            UpdateMarkButtonTexts();
        }

        if (_owner != null)
            _owner.ChangeSnapshotSelection();
    }

    public void OnGUI()
    {
        bool savedState = GUI.enabled;

        GUI.enabled = !_isDiffing;
        Do_GUI();

        GUILayout.FlexibleSpace();

        GUI.enabled = _snapshots.Count > 1 && _1st != _selected && _2nd != _selected;
        if (GUILayout.Button(_1stMarkText, EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            _1st = _selected;
            UpdateMarkButtonTexts();
        }
        if (GUILayout.Button(_2ndMarkText, EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            _2nd = _selected;
            UpdateMarkButtonTexts();
        }

        GUI.enabled = _1st != PAEditorConst.BAD_ID && _2nd != PAEditorConst.BAD_ID && !_isDiffing;
        if (GUILayout.Button("Diff", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            _isDiffing = true;

            Debug.LogFormat("diff {0} & {1}...", _1st, _2nd);
            if (_owner != null)
                _owner.BeginDiff();
        }

        GUI.enabled = _isDiffing;
        if (GUILayout.Button("End Diff", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            _isDiffing = false;

            if (_owner != null)
                _owner.EndDiff();
        }
        GUI.enabled = savedState;
    }

    public void Clear()
    {
        _snapshots.Clear();
        _indices = null;
        _selected = PAEditorConst.BAD_ID;
        _sessionTimeStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

        if (_owner != null)
            _owner.ClearSnapshots();
    }

    public void SetOwner(TrackerModeOwner owner) { _owner = owner; }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }
    public virtual void OnAppStarted() { }

    public virtual void Update() { }

    protected virtual void Do_GUI() { }

    public virtual bool SaveSessionInfo(PackedMemorySnapshot packed) { return false; }
    public virtual bool SaveSessionJson(CrawledMemorySnapshot Unpacked) { return false; }

    protected CrawledMemorySnapshot GetAt(int i)
    {
        CrawledMemorySnapshot unpacked = null;

        if (i >= 0 && i < _snapshots.Count)
        {
            unpacked = _snapshots[i].Unpacked;
        }

        return unpacked;
    }

    protected void RefreshIndices()
    {
        if (_snapshots == null || _snapshots.Count == 0)
        {
            _indices = null;
            return;
        }

        _indices = new string[_snapshots.Count];
        for (int i = 0; i < _snapshots.Count; i++)
        {
            bool isSelectedForDiff = i == _1st || i == _2nd;

            string delta = "-";
            if (i > 0)
            {
                int deltaInBytes = _snapshots[i].TotalSize - _snapshots[i - 1].TotalSize;
                delta = MemUtil.GetSign(deltaInBytes) + EditorUtility.FormatBytes(Mathf.Abs(deltaInBytes));
            }

            _indices[i] = string.Format("{0}{1} ({2})", i.ToString(), isSelectedForDiff ? "*" : "", delta);
        }
    }

    protected float DrawIndicesGrid(float initX, float initY)
    {
        float totalWidth = 0.0f;
        if (_indices != null)
        {
            totalWidth = 100 * _indices.Length;
            var newIndex = GUI.SelectionGrid(new Rect(initX, initY, totalWidth, 20), _selected, _indices, _indices.Length, MemStyles.ToolbarButton);
            if (newIndex != _selected)
            {
                _selected = newIndex;

                if (_owner != null)
                    _owner.ChangeSnapshotSelection();
            }
        }
        return totalWidth;
    }

    protected List<MemSnapshotInfo> _snapshots = new List<MemSnapshotInfo>();
    protected string[] _indices = null;
    protected int _selected = PAEditorConst.BAD_ID;

    protected int _1st = PAEditorConst.BAD_ID;
    protected int _2nd = PAEditorConst.BAD_ID;
    protected string _1stMarkText = MemConst.DiffMarkText_1st;
    protected string _2ndMarkText = MemConst.DiffMarkText_2nd;

    protected bool _saveIncomingSnapshot = false;
    protected string _sessionTimeStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

    protected bool _isDiffing = false;

    protected void UpdateMarkButtonTexts()
    {
        if (_1st != PAEditorConst.BAD_ID)
        {
            _1stMarkText = string.Format("{0} ({1})", MemConst.DiffMarkText_1st, _1st);
        }
        else
        {
            _1stMarkText = MemConst.DiffMarkText_1st;
        }
        if (_2nd != PAEditorConst.BAD_ID)
        {
            _2ndMarkText = string.Format("{0} ({1})", MemConst.DiffMarkText_2nd, _2nd);
        }
        else
        {
            _2ndMarkText = MemConst.DiffMarkText_2nd;
        }

        RefreshIndices();
    }

    protected TrackerModeOwner _owner;
}
