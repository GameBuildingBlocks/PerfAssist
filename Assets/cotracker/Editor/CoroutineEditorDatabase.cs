using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineInfo
{
    public CoroutineCreation creation;
    public CoroutineTermination termination;

    public List<KeyValuePair<float, float>> executions = new List<KeyValuePair<float,float>>();
}

public class CoroutineEditorDatabase 
{
    public static float SnapshotInterval = 1.0f;

    public static CoroutineEditorDatabase Instance = new CoroutineEditorDatabase();

    public CoroutineEditorDatabase()
    {
        CoGraphUtil.SetHeight(CoGraphUtil.GName_Creation, 50);
        CoGraphUtil.SetHeight(CoGraphUtil.GName_Termination, 50);
        CoGraphUtil.SetHeight(CoGraphUtil.GName_ExecCount, 100);
        CoGraphUtil.SetHeight(CoGraphUtil.GName_ExecTime, 200);
    }

    public void Receive(List<CoroutineActivity> activities)
    {
        foreach (var item in activities)
        {
            _activityQueue.Enqueue(item);
        }

        if (Time.realtimeSinceStartup - _lastSnapshotTime > SnapshotInterval)
        {
            if (Mathf.Approximately(_lastSnapshotTime, 0.0f))
            {
                CreateSnapshot(Time.realtimeSinceStartup);
                _lastSnapshotTime = Time.realtimeSinceStartup;
            }
            else
            {
                float newTime = _lastSnapshotTime + SnapshotInterval;
                CreateSnapshot(newTime);
                _lastSnapshotTime = newTime;
            }
        }
    }

    public void CreateSnapshot(float snapshotTime)
    {
        int creationCount = 0;
        int executionCount = 0;
        int terminationCount = 0;
        float executionTime = 0.0f;

        HashSet<int> curSnapshot = new HashSet<int>();
        _snapshots.Add(new KeyValuePair<float, HashSet<int>>(snapshotTime, curSnapshot));

        while (_activityQueue.Count > 0)
        {
            CoroutineActivity activity = _activityQueue.Dequeue();
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

            curSnapshot.Add(activity.seqID);

            CoroutineInfo info;
            if (!_coroutines.TryGetValue(activity.seqID, out info))
            {
                if (!(activity is CoroutineCreation))
                {
                    Debug.LogErrorFormat("activity ({0}:{1}) not found (possibly CoroutineCreation missing), ignored.", activity.seqID, activity.ToString());
                    continue;
                }

                info = new CoroutineInfo();
                _coroutines[activity.seqID] = info;
            }

            if (activity is CoroutineCreation)
            {
                creationCount++;
                info.creation = activity as CoroutineCreation;
            }
            else if (activity is CoroutineExecution)
            {
                var exec = activity as CoroutineExecution;

                executionCount++;
                executionTime += exec.timeConsumed;

                info.executions.Add(new KeyValuePair<float, float>(exec.timestamp, exec.timeConsumed));
            }
            else if (activity is CoroutineTermination)
            {
                terminationCount++;
                info.termination = activity as CoroutineTermination;
            }
        }

        // 这个函数一定要在 CoGraphUtil.LogData() 也就是 step 之前调用（否则 mCurrentIndex 就变到下一帧去了）
        CoGraphUtil.RecordSnapshot(snapshotTime);

        CoGraphUtil.LogData(CoGraphUtil.GName_Creation, creationCount);
        CoGraphUtil.LogData(CoGraphUtil.GName_Termination, terminationCount);
        CoGraphUtil.LogData(CoGraphUtil.GName_ExecCount, executionCount);
        CoGraphUtil.LogData(CoGraphUtil.GName_ExecTime, executionTime);
    }

    HashSet<int> FindSnapshotCoroutines(float snapshotTime)
    {
        foreach (var item in _snapshots)
        {
            if (Mathf.Approximately(snapshotTime, item.Key))
            {
                return item.Value;
            }
        }

        return null;
    }

    public List<CoTableEntry> PopulateEntries(float snapshotTime)
    {
        HashSet<int> coIDs = FindSnapshotCoroutines(snapshotTime);
        if (coIDs == null || coIDs.Count == 0)
            return null;

        List<CoTableEntry> ret = new List<CoTableEntry>();

        foreach (var id in coIDs)
        {
            CoroutineInfo info;
            if (_coroutines.TryGetValue(id, out info))
            {
                CoTableEntry entry = new CoTableEntry();
                entry.Name = info.creation.mangledName;
                if (info.executions.Count > 0)
                {
                    ExtractCoroutineSelectedInfo(snapshotTime, info,
                        out entry.ExecSelectedCount, 
                        out entry.ExecSelectedTime, 
                        out entry.ExecAccumCount, 
                        out entry.ExecAccumTime);
                }
                ret.Add(entry);
            }
            else
            {
                Debug.LogErrorFormat("coroutine {0} not found in database.", id);
            }
        }
        return ret;
    }

    void ExtractCoroutineSelectedInfo(float snapshot, CoroutineInfo info, out int selectedCnt, out float selected, out int accumCnt, out float accumTime)
    {
        selectedCnt = 0;
        selected = 0.0f;
        accumCnt = 0;
        accumTime = 0.0f;

        foreach (var item in info.executions)
        {
            if (item.Key <= snapshot)
            {
                if (item.Key > snapshot - SnapshotInterval)
                {
                    selected += item.Value;
                    selectedCnt++;
                }

                accumTime += item.Value;
                accumCnt++;
            }
        }
    }

    Queue<CoroutineActivity> _activityQueue = new Queue<CoroutineActivity>();

    List<KeyValuePair<float, HashSet<int>>> _snapshots = new List<KeyValuePair<float, HashSet<int>>>();
    Dictionary<int, CoroutineInfo> _coroutines = new Dictionary<int,CoroutineInfo>();

    float _lastSnapshotTime = 0.0f;
}
