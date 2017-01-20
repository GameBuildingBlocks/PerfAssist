using UnityEngine;
using System.Collections;
using MemoryProfilerWindow;
using System.Collections.Generic;

public enum TrackerMode
{
    Editor,
    Remote,
    Daemon,
    File,
}

public class TrackerModeManager
{
    public TrackerMode CurrentMode { get { return _currentMode; } }
    TrackerMode _currentMode;

    public CrawledMemorySnapshot SelectedUnpacked { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.SelectedUnpacked : null ; } }
    public CrawledMemorySnapshot PrevUnpacked { get { var curMode = GetCurrentMode(); return curMode != null ? curMode.PrevUnpacked : null; } }

    public void OnGUI()
    {
        var curMode = GetCurrentMode();
        if (curMode != null)
            curMode.OnGUI();
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
        { TrackerMode.Daemon, new TrackerMode_Daemon() },
        { TrackerMode.File, new TrackerMode_File() },
    };
}
