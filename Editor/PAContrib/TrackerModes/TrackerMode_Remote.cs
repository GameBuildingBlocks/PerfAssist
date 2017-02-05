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

    bool _connectPressed = false;

    public override void Update()
    {
        if (_connectPressed)
        {
            TrackerModeUtil.Connect(_IPField);
            _connectPressed = false;
        }
    }

    public override void OnGUI()
    {
        GUI.SetNextControlName("LoginIPTextField");
        var currentStr = GUILayout.TextField(_IPField, GUILayout.Width(100));
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
        if (GUILayout.Button("Connect", GUILayout.Width(80)))
        {
            _connectPressed = true;
        }
        GUI.enabled = connected;
        if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
        {
            MemorySnapshot.RequestNewSnapshot();
        }
        GUI.enabled = savedState;

        GUILayout.Space(DrawIndicesGrid(300, 20));
        GUILayout.FlexibleSpace();

        _autoSaveToggle = GUILayout.Toggle(_autoSaveToggle, new GUIContent("AutoSave"), GUILayout.MaxWidth(80));

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
