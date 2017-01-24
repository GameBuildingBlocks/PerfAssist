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

            RefreshIndices();
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

        string recordTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", new System.Globalization.DateTimeFormatInfo());
        string snapshotFilePath = Path.Combine(MemUtil.SnapshotsDir, recordTime + MemConst.EditorFolderStrFlag);
        string snapshotFileName = Path.Combine(snapshotFilePath, string.Format(MemConst.SnapshotBinFileNameFormatter, _selected));

        if (!TrackerModeUtil.SaveSnapshotBin(snapshotFilePath, snapshotFileName, packed))
        {
            Debug.LogErrorFormat("Save Snapshot Bin Failed! recordTime = {0}", recordTime);
            return false;
        }
        Debug.LogFormat("Save Snapshot Bin Suc! recordTime = {0}", recordTime);

        string jsonFilePath = Path.Combine(snapshotFilePath, "json");
        string jsonFileName = Path.Combine(jsonFilePath, string.Format(MemConst.SnapshotJsonFileNameFormatter, _selected));
        if (!TrackerModeUtil.SaveSnapshotJson(jsonFilePath,jsonFileName,unpacked))
        {
            Debug.LogErrorFormat("Save Snapshot Json Failed! recordTime = {0}", recordTime);
            return false;
        }
        Debug.LogFormat("Save Snapshot Json Suc! recordTime = {0}", recordTime);
        return true;
    }
}
