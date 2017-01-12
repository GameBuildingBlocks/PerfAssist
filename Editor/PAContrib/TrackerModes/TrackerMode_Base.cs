using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MemoryProfilerWindow;

public delegate void SelectionChangeHandler();

public class TrackerMode_Base
{
    public CrawledMemorySnapshot SelectedUnpacked { get { return GetAt(_selected); } }
    public CrawledMemorySnapshot PrevUnpacked { get { return _selected > 0 ? GetAt(_selected - 1) : null; } }

    public SelectionChangeHandler SelectionChanged;

    public virtual void OnGUI() { }

    protected void Clear()
    {
        _snapshots.Clear();
        _indices = null;
        _selected = PAEditorConst.BAD_ID;
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

    protected List<MemSnapshotInfo> _snapshots = new List<MemSnapshotInfo>();
    protected string[] _indices = null;
    protected int _selected = PAEditorConst.BAD_ID;
}
