using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class CoroutineActivity
{
    public int seqID;
    public float timestamp;

    public CoroutineActivity()
    {
        timestamp = Time.realtimeSinceStartup;
    }
}

public class CoroutineCreation : CoroutineActivity
{
    public string mangledName;
    public string stacktrace;

    public CoroutineCreation(int seq) : base() { }
}

public class CoroutineExecution : CoroutineActivity
{
    public float timeConsumed;

    public CoroutineExecution(int seq) : base() { }
}

public class CoroutineTermination : CoroutineActivity
{
    public CoroutineTermination(int seq) : base () {}
}

public delegate void OnCoStatsBroadcast(List<CoroutineActivity> activities);

public class CoroutineStatisticsV2 
{
    public static CoroutineStatisticsV2 Instance = new CoroutineStatisticsV2();

    public void MarkCreation(int seq, string mangledName)
    {
        if (!_broadcastStarted)
        {
            Debug.LogErrorFormat("[CoStats] error: invalid broadcast while coroutine '{0}' is being created, ignored.", seq);
            return;
        }

        CoroutineCreation creation = new CoroutineCreation(seq);
        creation.mangledName = mangledName;
        creation.stacktrace = StackTraceUtility.ExtractStackTrace();
        _activities.Add(creation);
        _activeCoroutines.Add(seq);
    }

    public void MarkMoveNext(int seq, float timeConsumed)
    {
        if (!_broadcastStarted)
        {
            Debug.LogErrorFormat("[CoStats] error: invalid broadcast while coroutine '{0}' is performing MoveNext(), ignored.", seq);
            return;
        }

        if (!_activeCoroutines.Contains(seq))
        {
            Debug.LogErrorFormat("[CoStats] error: coroutine '{0}' is performing MoveNext() but could not be found in '_activeCoroutines', ignored.", seq);
            return;
        }

        CoroutineExecution exec = new CoroutineExecution(seq);
        exec.timeConsumed = timeConsumed;
        _activities.Add(exec);
    }

    public void MarkTermination(int seq)
    {
        if (!_broadcastStarted)
        {
            Debug.LogErrorFormat("[CoStats] error: invalid broadcast while coroutine '{0}' is being terminated, ignored.", seq);
            return;
        }

        if (!_activeCoroutines.Contains(seq))
        {
            Debug.LogErrorFormat("[CoStats] error: coroutine '{0}' is terminating but could not be found in '_activeCoroutines', ignored.", seq);
            return;
        }

        _activities.Add(new CoroutineTermination(seq));
        _activeCoroutines.Remove(seq);
    }


    public IEnumerator BroadcastCoroutine()
    {
        _broadcastStarted = true;

        while (true)
        {
            if (hasBroadcastReceivers())
                _onBroadcast(_activities);

            _activities.Clear();

            yield return new WaitForSeconds((float)CoroutineRuntimeTrackingConfig.BroadcastInterval);
        }
    }

    private OnCoStatsBroadcast _onBroadcast;
    public event OnCoStatsBroadcast OnBroadcast
    {
        add
        {
            _onBroadcast -= value;
            _onBroadcast += value;
        }
        remove
        {
            _onBroadcast -= value;
        }
    }

    List<CoroutineActivity> _activities = new List<CoroutineActivity>();
    HashSet<int> _activeCoroutines = new HashSet<int>();

    bool _broadcastStarted = false;
    bool hasBroadcastReceivers() { return _onBroadcast != null && _onBroadcast.GetInvocationList().Length > 0; }
}
