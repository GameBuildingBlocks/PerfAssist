using MemoryProfilerWindow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEngine;

#if JX3M

public class TrackerMode_RemoteEx : TrackerMode_Base
{
    public TrackerMode_RemoteEx()
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
            RemoteSwitchClient.Instance.Connect(_IPField, 10000);
            EditorPrefs.SetString(MemPrefs.LastConnectedIP, _IPField);
            Debug.LogWarningFormat("-> request RemoteSwitchClient.Connect('{0}')", _IPField);
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

        bool connected = RemoteSwitchClient.Instance.IsConnected;

        GUI.enabled = !connected;
        if (GUILayout.Button("Connect", GUILayout.Width(80)))
        {
            _connectPressed = true;
        }
        GUI.enabled = connected;
        if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
        {
            RemoteSwitchClient.Instance.DoTakeMemorySnapshot();
        }
        GUI.enabled = savedState;

        GUILayout.Space(DrawIndicesGrid(300, 20));

        // handle file-receiving 
        string recvFilePath = RemoteSwitchClient.Instance.RecvFilePath;
        if (!string.IsNullOrEmpty(recvFilePath))
        {
            try
            {
                U3DPerfProfiler.MemorySnapshotAnalyzer.AnalyzeMemorySnapshot(recvFilePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogErrorFormat("processing received file failed. ('{0}')", recvFilePath);
                Debug.LogException(ex);
            }
            finally
            {
                RemoteSwitchClient.Instance.DestroyMemorySnapShotFile(recvFilePath);
                RemoteSwitchClient.Instance.RecvFilePath = null;
            }
        }
    }
}

#endif
