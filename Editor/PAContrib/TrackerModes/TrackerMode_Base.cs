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
    }

    public virtual void OnEnter() { }
    public virtual void OnLeave() { }
    public virtual void OnAppStarted() { }

    public virtual void Update() { }
    public virtual void OnGUI() { }

    public virtual bool SaveSessionInfo(PackedMemorySnapshot packed, CrawledMemorySnapshot unpacked) { return false;}

    protected void Clear()
    {
        _snapshots.Clear();
        _indices = null;
        _selected = PAEditorConst.BAD_ID;
        _sessionTimeStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
}

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

        if (_indices == null || _indices.Length != _snapshots.Count)
        {
            _indices = new string[_snapshots.Count];
            for (int i = 0; i < _snapshots.Count; i++)
            {
                _indices[i] = i.ToString();
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

    protected bool _saveIncomingSnapshot = false;
    protected string _sessionTimeStr = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
}
