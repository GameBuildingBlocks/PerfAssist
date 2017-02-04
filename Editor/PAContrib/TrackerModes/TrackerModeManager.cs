using MemoryProfilerWindow;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrackerMode
{
    Editor,
    Remote,
    File,
}

public class TrackerModeManager 
{
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

    public void OnGUI()
    {
        try
        {
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
            GUILayout.EndHorizontal();

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
