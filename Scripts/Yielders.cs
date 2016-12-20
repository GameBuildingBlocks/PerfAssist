using System.Collections.Generic;
using UnityEngine;

// Usage:
//    yield return new WaitForEndOfFrame();     =>      yield return Yielders.EndOfFrame;
//    yield return new WaitForFixedUpdate();    =>      yield return Yielders.FixedUpdate;
//    yield return new WaitForSeconds(1.0f);    =>      yield return Yielders.GetWaitForSeconds(1.0f);

// http://forum.unity3d.com/threads/c-coroutine-waitforseconds-garbage-collection-tip.224878/

public static class Yielders
{
    public static bool Enabled = true;  

    public static int _internalCounter = 0; // counts how many times the app yields

    // dictionary with a key of ValueType will box the value to perform comparison / hash code calculation while scanning the hashtable.
    // here we implement IEqualityComparer<float> and pass it to your dictionary to avoid that GC
    class FloatComparer : IEqualityComparer<float>
    {
        bool IEqualityComparer<float>.Equals(float x, float y)
        {
            return x == y;
        }
        int IEqualityComparer<float>.GetHashCode(float obj)
        {
            return obj.GetHashCode();
        }
    }

    static WaitForEndOfFrame _endOfFrame = new WaitForEndOfFrame();
    public static WaitForEndOfFrame EndOfFrame
    {
        get { _internalCounter++; return Enabled ? _endOfFrame : new WaitForEndOfFrame(); }
    }

    static WaitForFixedUpdate _fixedUpdate = new WaitForFixedUpdate();
    public static WaitForFixedUpdate FixedUpdate
    {
        get { _internalCounter++; return Enabled ? _fixedUpdate : new WaitForFixedUpdate(); }
    }

    public static WaitForSeconds GetWaitForSeconds(float seconds)
    {
        _internalCounter++; 

        if (!Enabled)
            return new WaitForSeconds(seconds);

        WaitForSeconds wfs;

        // it is always better to use TryGetValue() method instead of dict.Contains(key) + dict[key] 
        // since it performs what you want with a single 'pass' through hashtable.
        if (!_waitForSecondsYielders.TryGetValue(seconds, out wfs))
            _waitForSecondsYielders.Add(seconds, wfs = new WaitForSeconds(seconds));

        return wfs;
    }

    public static void ClearWaitForSeconds()
    {
        _waitForSecondsYielders.Clear();
    }

    static Dictionary<float, WaitForSeconds> _waitForSecondsYielders = new Dictionary<float, WaitForSeconds>(100, new FloatComparer());
}
