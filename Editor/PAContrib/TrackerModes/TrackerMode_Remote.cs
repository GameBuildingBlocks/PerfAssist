using MemoryProfilerWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

public class TrackerMode_Remote : TrackerMode_Base
{
    bool _autoSaveToggle = true;
    string _IPField = MemConst.RemoteIPDefaultText;

    public override void OnGUI()
    {
        GUI.SetNextControlName("LoginIPTextField");
        var currentStr = GUILayout.TextField(_IPField, GUILayout.Width(80));
        if (!_IPField.Equals(currentStr))
        {
            _IPField = currentStr;
        }

        if (GUI.GetNameOfFocusedControl().Equals("LoginIPTextField") && _IPField.Equals(MemConst.RemoteIPDefaultText))
        {
            _IPField = "";
        }

        bool savedState = GUI.enabled;

        bool connected = NetManager.Instance != null && NetManager.Instance.IsConnected && MemUtil.IsProfilerConnectedRemotely;

        GUI.enabled = !connected;
        if (GUILayout.Button("Connect", GUILayout.Width(60)))
        {
            TrackerModeUtil.Connect(_IPField);
        }
        GUI.enabled = connected;
        if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
        {
            MemorySnapshot.RequestNewSnapshot();
        }
        GUI.enabled = savedState;

        GUILayout.Space(DrawIndicesGrid(250, 20));
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

        string sessionName = _sessionTimeStr + TrackerModeConsts.RemoteTag + _IPField;
        return TrackerModeUtil.SaveSnapshotFiles(sessionName, _selected.ToString(), packed, unpacked);
    }
}
