using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineEditorReceived 
{
    public static float SnapshotInterval = 1.0f;

    public static CoroutineEditorReceived Instance = new CoroutineEditorReceived();

    public CoroutineEditorReceived()
    {
        CoroutineGraphUtil.SetHeight("creation", 50);
        CoroutineGraphUtil.SetHeight("termination", 50);
        CoroutineGraphUtil.SetHeight("exec_count", 100);
        CoroutineGraphUtil.SetHeight("exec_time", 200);
    }

    public void Receive(List<CoroutineActivity> activities)
    {
        foreach (var item in activities)
        {
            _activities.Enqueue(item);
        }

        if (Time.realtimeSinceStartup - _lastSnapshotTime > SnapshotInterval)
        {
            float newTime = _lastSnapshotTime + SnapshotInterval;
            CreateSnapshot(newTime);
            _lastSnapshotTime = newTime;
        }
    }

    public void CreateSnapshot(float snapshotTime)
    {
        int creationCount = 0;
        int executionCount = 0;
        int terminationCount = 0;
        float executionTime = 0.0f;

        while (_activities.Count > 0)
        {
            CoroutineActivity activity = _activities.Dequeue();
            if (activity.timestamp < _lastSnapshotTime)
            {
                Debug.LogErrorFormat("[CoEd] error: {0} is earlier than last snapshot, discarded. (activity_time: {1:0.000}, last_snapshot: {2:0.000})",
                    activity.GetType().ToString(), activity.timestamp, _lastSnapshotTime);
                continue;
            }
            else if (activity.timestamp >= snapshotTime)
            {
                // stop current activity
                break;
            }

            if (activity is CoroutineCreation)
            {
                creationCount++;
            }
            else if (activity is CoroutineExecution)
            {
                var exec = activity as CoroutineExecution;

                executionCount++;
                executionTime += exec.timeConsumed;
            }
            else if (activity is CoroutineTermination)
            {
                terminationCount++;
            }
        }

        CoroutineGraphUtil.LogData("creation", creationCount);
        CoroutineGraphUtil.LogData("termination", terminationCount);
        CoroutineGraphUtil.LogData("exec_count", executionCount);
        CoroutineGraphUtil.LogData("exec_time", executionTime);
    }

    Queue<CoroutineActivity> _activities = new Queue<CoroutineActivity>();

    float _lastSnapshotTime = 0.0f;
}
