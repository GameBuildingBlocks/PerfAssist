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
    public TrackerMode_Remote()
    {
    }

    string _IPField = MemConst.RemoteIPDefaultText;

    bool _connectPressed = false;

    public override void OnEnter()
    {
        string saved = EditorPrefs.GetString(MemPrefs.LastConnectedIP);
        if (!string.IsNullOrEmpty(saved))
        {
            _IPField = saved;
        }
    }

    public override void Update()
    {
        if (_connectPressed)
        {
            TrackerModeUtil.Connect(_IPField);
            _connectPressed = false;
        }
    }

    protected override void Do_GUI()
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

        bool connected = /*NetManager.Instance != null && NetManager.Instance.IsConnected &&*/ MemUtil.IsProfilerConnectedRemotely;

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
    }

    public override bool SaveSessionInfo(PackedMemorySnapshot packed)
    {
        string sessionName = _sessionTimeStr + TrackerModeConsts.RemoteTag + _IPField;
        return TrackerModeUtil.SaveSnapshotFiles(sessionName, _selected.ToString(), packed);
    }


    public override bool SaveSessionJson(CrawledMemorySnapshot Unpacked)
    {
        string sessionName = _sessionTimeStr + TrackerModeConsts.RemoteTag + _IPField;
        return TrackerModeUtil.SaveSnapshotJson(sessionName, _selected.ToString() + ".json", Unpacked);
    }
}