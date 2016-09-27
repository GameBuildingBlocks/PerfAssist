using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CoroutineEditorReceived 
{
    public static float SnapshotInterval = 3.0f;

    public static CoroutineEditorReceived Instance = new CoroutineEditorReceived();

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
        // 这里需要做下面的事情：

        //1. 三秒内总的创建数，销毁数，执行数
        //2. 总的执行时间
    }

    Queue<CoroutineActivity> _activities;

    float _lastSnapshotTime = 0.0f;
}
