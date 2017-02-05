using UnityEngine;
using System.Collections;

public delegate void OwnerGeneralEventHandler();

public class TrackerModeOwner
{
    public event OwnerGeneralEventHandler OnSnapshotsCleared;
    public event OwnerGeneralEventHandler OnSnapshotSelectionChanged;
    public event OwnerGeneralEventHandler OnSnapshotDiffBegin;
    public event OwnerGeneralEventHandler OnSnapshotDiffEnd;

    public void ClearSnapshots()
    {
        if (OnSnapshotsCleared != null)
            OnSnapshotsCleared();
    }

    public void ChangeSnapshotSelection()
    {
        if (OnSnapshotSelectionChanged != null)
            OnSnapshotSelectionChanged();
    }

    public void BeginDiff()
    {
        if (OnSnapshotDiffBegin != null)
            OnSnapshotDiffBegin();
    }
    public void EndDiff()
    {
        if (OnSnapshotDiffEnd != null)
            OnSnapshotDiffEnd();
    }
}
