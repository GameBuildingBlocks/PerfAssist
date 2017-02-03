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
    bool _autoSaveToggle = true;

    public override void OnAppStarted()
    {
        TrackerModeUtil.Connect(MemConst.LocalhostIP);
    }

    public override void OnGUI()
    {
        if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
        {
            MemorySnapshot.RequestNewSnapshot();
        }

        GUILayout.Space(DrawIndicesGrid(120, 20));
        GUILayout.FlexibleSpace();

        _autoSaveToggle = GUILayout.Toggle(_autoSaveToggle, new GUIContent("AutoSave"), GUILayout.MaxWidth(80));

        if (GUILayout.Button("Clear Session", GUILayout.MaxWidth(100)))
        {
            Clear();
        }

        if (GUILayout.Button("Open Dir", GUILayout.MaxWidth(80)))
        {
            EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
        }
    }

    public override bool SaveSessionInfo(PackedMemorySnapshot packed, CrawledMemorySnapshot unpacked)
    {
        if (!_autoSaveToggle)
            return false;

        string sessionName = _sessionTimeStr + TrackerModeConsts.EditorTag;
        return TrackerModeUtil.SaveSnapshotFiles(sessionName, _selected.ToString(), packed, unpacked);
    }
}
