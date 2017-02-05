using MemoryProfilerWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class TrackerMode_Editor : TrackerMode_Base
{
    public override void OnAppStarted()
    {
        TrackerModeUtil.Connect(MemConst.LocalhostIP);
    }

    Rect _last;

    protected override void Do_GUI()
    {
        if (GUILayout.Button("Take Snapshot", EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(20)))
        {
            MemorySnapshot.RequestNewSnapshot();
        }

        if (Event.current.type == EventType.Repaint)
            _last = GUILayoutUtility.GetLastRect();

        GUILayout.Space(DrawIndicesGrid(_last.xMax + 20, _last.y));
    }

    public override bool SaveSessionInfo(PackedMemorySnapshot packed, CrawledMemorySnapshot unpacked)
    {
        string sessionName = _sessionTimeStr + TrackerModeConsts.EditorTag;
        return TrackerModeUtil.SaveSnapshotFiles(sessionName, _selected.ToString(), packed, unpacked);
    }
}
