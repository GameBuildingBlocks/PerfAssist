using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

public class CoroutineRuntimeTrackingConfig
{
    // enable the whole tracking system
    public static bool EnableTracking = false;

    // enables Profiler.BeginSample()/EndSample() for each enumeration of coroutines
    public static bool EnableProfiling = false;

    // enables counting of coroutines creation and enumeration
    public static bool EnableCounting = false;

    // last n seconds are kept
    public static float KeptSeconds = 3.0f;
}

public enum CoStatsEvent
{
    Creation,
    Enumeration,
}

public struct CoStatsEntry
{
    public float timestamp;
    public string coId;
    public CoStatsEvent coEvt;
}

public class CoroutineStatistics
{
    public static void MarkEvent(string coIdentifier, CoStatsEvent coEvent)
    {
        _history.Add(new CoStatsEntry() { timestamp = Time.time, coId = coIdentifier, coEvt = coEvent });
    }

    public static void ReportAndCleanup()
    {
        int _lastnSecCreationCount = 0;
        int _lastnSecEnumerationCount = 0;

        for (int i = 0; i < _history.Count; i++)
        {
            switch (_history[i].coEvt)
            {
                case CoStatsEvent.Creation:
                    _lastnSecCreationCount++;
                    break;
                case CoStatsEvent.Enumeration:
                    _lastnSecEnumerationCount++;
                    break;
                default:
                    break;
            }
        }

        _history.Clear();
        Debug.LogFormat("[CoStats] {0} created, {1} enumerated.", _lastnSecCreationCount, _lastnSecEnumerationCount);
    }

    static List<CoStatsEntry> _history = new List<CoStatsEntry>();
}

public class TrackedCoroutine : IEnumerator
{
    IEnumerator _routine;

    public TrackedCoroutine(IEnumerator routine)
    {
        _routine = routine;

        if (CoroutineRuntimeTrackingConfig.EnableCounting)
            CoroutineStatistics.MarkEvent(_routine.GetType().ToString(), CoStatsEvent.Creation);
    }

    object IEnumerator.Current
    {
        get
        {
            return _routine.Current;
        }
    }

    public bool MoveNext()
    {
        if (CoroutineRuntimeTrackingConfig.EnableCounting)
            CoroutineStatistics.MarkEvent(_routine.GetType().ToString(), CoStatsEvent.Enumeration);

        if (CoroutineRuntimeTrackingConfig.EnableProfiling)
            Profiler.BeginSample(_routine.GetType().ToString());

        bool succ = _routine.MoveNext();

        if (CoroutineRuntimeTrackingConfig.EnableProfiling)
            Profiler.EndSample();

        return succ;
    }

    public void Reset()
    {
        _routine.Reset();
    }
}

public class RuntimeCoroutineTracker
{
    public static Coroutine InvokeStart(MonoBehaviour initiator, IEnumerator routine)
    {
        if (!CoroutineRuntimeTrackingConfig.EnableTracking)
            return initiator.StartCoroutine(routine);

        try
        {
            return initiator.StartCoroutine(new TrackedCoroutine(routine));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    public static Coroutine InvokeStart(MonoBehaviour initiator, string methodName)
    {
        if (!CoroutineRuntimeTrackingConfig.EnableTracking)
            return initiator.StartCoroutine(methodName);

        try
        {
            Type type = initiator.GetType();
            if (type == null)
                throw new ArgumentNullException("initiator", "invalid initiator (null type)");

            MethodInfo coroutineMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (coroutineMethod == null)
                throw new ArgumentNullException("methodName", string.Format("Invalid method {0} (method not found)", methodName));

            IEnumerator coroutineEnumerator = coroutineMethod.Invoke(initiator, null) as IEnumerator;
            if (coroutineEnumerator == null)
                throw new ArgumentNullException("methodName", string.Format("Invalid method {0} (not an IEnumerator)", methodName));

            return InvokeStart(initiator, coroutineEnumerator);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    public static void EnableTrackingSystemProgrammatically()
    {
        CoroutineRuntimeTrackingConfig.EnableTracking = true;
        CoroutineRuntimeTrackingConfig.EnableProfiling = true;
        CoroutineRuntimeTrackingConfig.EnableCounting = true;
    }

    public static IEnumerator DefaultStatsReportCoroutine()
    {
        while (true)
        {
            CoroutineStatistics.ReportAndCleanup();

            yield return new WaitForSeconds((float)CoroutineRuntimeTrackingConfig.KeptSeconds);
        }
    }
}
