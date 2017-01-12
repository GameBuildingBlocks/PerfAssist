using UnityEngine;
using System.Collections;
using MemoryProfilerWindow;

public class TrackerModeManager
{
    public CrawledMemorySnapshot SelectedUnpacked { get { return _trackerModeFile.SelectedUnpacked; } }
    public CrawledMemorySnapshot PrevUnpacked { get { return _trackerModeFile.PrevUnpacked; } }

    public void OnGUI()
    {
        _trackerModeFile.OnGUI();
    }

    public void SetSelectionChanged(SelectionChangeHandler handler)
    {
        _trackerModeFile.SelectionChanged = handler;
    }

    TrackerMode_File _trackerModeFile = new TrackerMode_File();
}
