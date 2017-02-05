using MemoryProfilerWindow;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum TrackerMode
{
    Editor,
    Remote,
    File,
}

public delegate void SessionClearHandler();

public class TrackerModeManager 
{
    public event SessionClearHandler OnSessionSnapshotsCleared;

    public bool AutoSaveOnSnapshot { get { return _configWindow.AutoSaveOnSnapshot; } }

    public TrackerMode CurrentMode { get { return _currentMode; } }
    TrackerMode _currentMode = TrackerMode.Editor;

    public CrawledMemorySnapshot SelectedUnpacked { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.SelectedUnpacked : null ; } }
    public CrawledMemorySnapshot PrevUnpacked { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.PrevUnpacked : null; } }

    public void Update()
    {
        var curMode = GetCurrentMode();
        if (curMode != null)
            curMode.Update();
    }

    public void Clear()
    {
        foreach (var t in _modes.Values)
            t.Clear();
    }

    private MemConfigPopup _configWindow = new MemConfigPopup();
    private Rect _optionPopupRect;

    public void OnGUI()
    {
        try
        {
            // shared functionalities
            GUILayout.BeginHorizontal(MemStyles.Toolbar);
            int gridWidth = 250;
            int selMode = GUI.SelectionGrid(new Rect(0, 0, gridWidth, 20), (int)_currentMode,
                TrackerModeConsts.Modes, TrackerModeConsts.Modes.Length, MemStyles.ToolbarButton);
            if (selMode != (int)_currentMode)
            {
                SwitchTo((TrackerMode)selMode);
            }
            GUILayout.Space(gridWidth + 30);
            GUILayout.Label(TrackerModeConsts.ModesDesc[selMode]);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Clear Session", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
            {
                TrackerMode_Base mode = GetCurrentMode();
                if (mode != null)
                {
                    mode.Clear();

                    if (OnSessionSnapshotsCleared != null)
                        OnSessionSnapshotsCleared();
                }
            }
            if (GUILayout.Button("Open Dir", MemStyles.ToolbarButton, GUILayout.MaxWidth(100)))
            {
                EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
            }
            if (GUILayout.Button("Options", EditorStyles.toolbarDropDown, GUILayout.Width(100)))
            {
                try
                {
                    PopupWindow.Show(_optionPopupRect, _configWindow);
                }
                catch (ExitGUIException)
                {
                    // have no idea why Unity throws ExitGUIException() in GUIUtility.ExitGUI()
                    // so we silently ignore the exception 
                }
            }
            if (Event.current.type == EventType.Repaint)
                _optionPopupRect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // mode-specific controls
            GUILayout.BeginHorizontal();
            var curMode = GetCurrentMode();
            if (curMode != null)
                curMode.OnGUI();
            GUILayout.EndHorizontal();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    public void SetSelectionChanged(SelectionChangeHandler handler)
    {
        foreach (var t in _modes.Values)
            t.SelectionChanged = handler;
    }

    public TrackerMode_Base GetCurrentMode()
    {
        TrackerMode_Base mode;
        if (!_modes.TryGetValue(_currentMode, out mode))
            return null;
        return mode;
    }

    public void SwitchTo(TrackerMode newMode)
    {
        if (_currentMode == newMode)
            return;

        TrackerMode_Base mode = GetCurrentMode();
        if (mode != null)
            mode.OnLeave();

        _currentMode = newMode;
        mode = GetCurrentMode();
        if (mode != null)
            mode.OnEnter();
    }

    Dictionary<TrackerMode, TrackerMode_Base> _modes = new Dictionary<TrackerMode, TrackerMode_Base>()
    {
        { TrackerMode.Editor, new TrackerMode_Editor() },
        { TrackerMode.Remote, new TrackerMode_Remote() },
        { TrackerMode.File, new TrackerMode_File() },
    };
}
