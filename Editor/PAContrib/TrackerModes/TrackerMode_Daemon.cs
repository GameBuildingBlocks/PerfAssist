using MemoryProfilerWindow;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class TrackerMode_Daemon : TrackerMode_Base
{
    public override void OnGUI()
    {
        if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
        {
            MemorySnapshot.RequestNewSnapshot();
        }
    }

    public override bool SaveSessionInfo(PackedMemorySnapshot packed, CrawledMemorySnapshot unpacked)
    {
        string sessionName = _sessionTimeStr + TrackerModeConsts.DaemonTag;
        return TrackerModeUtil.SaveSnapshotFiles(sessionName, _selected.ToString(), packed, unpacked);
    }
}
