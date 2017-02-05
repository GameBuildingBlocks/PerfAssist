using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MemoryProfilerWindow;
using UnityEditorInternal;
using System;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using System.IO;

public delegate void SelectionChangeHandler();

public class TrackerMode_Base
{
    public CrawledMemorySnapshot SelectedUnpacked { get { return GetAt(_selected); } }
    public CrawledMemorySnapshot PrevUnpacked { get { return _selected > 0 ? GetAt(_selected - 1) : null; } }

    public SelectionChangeHandler SelectionChanged;

    public void AddSnapshot(MemSnapshotInfo snapshot)
    {
        _snapshots.Add(snapshot);
        _selected = _snapshots.Count - 1;

        SelectionChanged();
        RefreshIndices();

        // automatically compare the last two, when new snapshot comes
        if (_snapshots.Count > 1)
        {
            _1st = _selected - 1;
            _2nd = _selected;
            UpdateMarkButtonTexts();
        }
    }

    public void OnGUI()
    {
        Do_GUI();

        GUILayout.FlexibleSpace();

        bool savedState = GUI.enabled;
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
        GUI.enabled = _1st != PAEditorConst.BAD_ID && _2nd != PAEditorConst.BAD_ID;
        if (GUILayout.Button("Diff", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            Debug.LogFormat("diff {0} & {1}...", _1st, _2nd);
        }
        GUI.enabled = savedState;
    }

    public void Clear()
    {
        _snapshots.Clear();
        _indices = null;
        _selected = PAEditorConst.BAD_ID;
        _sessionTimeStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
    }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }
    public virtual void OnAppStarted() { }

    public virtual void Update() { }

    protected virtual void Do_GUI() { }

    public virtual bool SaveSessionInfo(PackedMemorySnapshot packed, CrawledMemorySnapshot unpacked) { return false;}

    protected CrawledMemorySnapshot GetAt(int i)
    {
        CrawledMemorySnapshot unpacked = null;

        if (i >= 0 && i < _snapshots.Count)
        {
            unpacked = _snapshots[i].unPacked;
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
            _indices[i] = i.ToString();

            if (i == _1st || i == _2nd)
            {
                _indices[i] += "*";
            }
        }
    }

    protected float DrawIndicesGrid(float initX, float initY)
    {
        float totalWidth = 0.0f;
        if (_indices != null)
        {
            totalWidth = 30 * _indices.Length;
            var newIndex = GUI.SelectionGrid(new Rect(initX, initY, totalWidth, 20), _selected, _indices, _indices.Length, MemStyles.ToolbarButton);
            if (newIndex != _selected)
            {
                _selected = newIndex;

                SelectionChanged();
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
}
