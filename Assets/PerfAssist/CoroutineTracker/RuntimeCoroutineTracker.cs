using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

#if UNITY_5_5_OR_NEWER
using UnityEngine.Profiling;
#endif

public class CoroutineRuntimeTrackingConfig
{
    // enable the whole tracking system
    public static bool EnableTracking = false;

    // last n seconds are kept
    public static float BroadcastInterval = 0.5f;
}

public class CoroutineNameCache
{
    public static string Mangle(string rawName)
    {
        string mangled;
        if (_mangledNames.TryGetValue(rawName, out mangled))
            return mangled;

        // (manual-mangling) which provides better readability
        mangled = rawName.Replace('<', '{').Replace('>', '}');

        // (auto-mangling) Url Escaping 
        //mangled = System.Uri.EscapeDataString(rawName);

        _mangledNames[rawName] = mangled;
        return mangled;
    }

    private static Dictionary<string, string> _mangledNames = new Dictionary<string,string>();
}

public class TrackedCoroutine : IEnumerator
{
    int _seqID;
    IEnumerator _routine;
    string _mangledName;

    static Stopwatch _stopWatch;

    public TrackedCoroutine(IEnumerator routine)
    {
        _routine = routine;
        _mangledName = CoroutineNameCache.Mangle(_routine.GetType().ToString());
        _seqID = _seqNext++;

        RuntimeCoroutineStats.Instance.MarkCreation(_seqID, _mangledName);
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
        Profiler.BeginSample(_mangledName);

        if (_stopWatch == null)
            _stopWatch = Stopwatch.StartNew();

        _stopWatch.Reset();
        _stopWatch.Start();

        bool next = _routine.MoveNext();

        _stopWatch.Stop();
        Profiler.EndSample();

        float timeConsumed = (float)((double)_stopWatch.ElapsedTicks / (double)Stopwatch.Frequency); 
        RuntimeCoroutineStats.Instance.MarkMoveNext(_seqID, timeConsumed);

        if (!next)
            RuntimeCoroutineStats.Instance.MarkTermination(_seqID);

        return next;
    }

    public void Reset()
    {
        _routine.Reset();
    }

    static int _seqNext = 0;
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
            UnityEngine.Debug.LogException(ex);
            return null;
        }
    }

    public static Coroutine InvokeStart(MonoBehaviour initiator, string methodName, object arg = null)
    {
        if (!CoroutineRuntimeTrackingConfig.EnableTracking)
            return initiator.StartCoroutine(methodName, arg);

        try
        {
            Type type = initiator.GetType();
            if (type == null)
                throw new ArgumentNullException("initiator", "invalid initiator (null type)");

            MethodInfo coroutineMethod = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
            if (coroutineMethod == null)
                throw new ArgumentNullException("methodName", string.Format("Invalid method {0} (method not found)", methodName));

            object[] args = null;
            if (arg != null)
            {
                args = new object[1] { arg };
            }

            IEnumerator coroutineEnumerator = coroutineMethod.Invoke(initiator, args) as IEnumerator;
            if (coroutineEnumerator == null)
                throw new ArgumentNullException("methodName", string.Format("Invalid method {0} (not an IEnumerator)", methodName));

            return InvokeStart(initiator, coroutineEnumerator);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogException(ex);
            return null;
        }
    }
}
