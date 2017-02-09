using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MemoryProfilerWindow;
using System;

public enum eDiffStatus
{
    Added,
    Increased,
    Unchanged,
    Decreased,
    Removed,
}

public class MemObjectInfoSet
{
    public static readonly string[] Prefixes = { "{added} ", "{increased} ", "{unchanged} ", "{decreased} ", "{removed} " };

    public MemObjectInfoSet(List<object> objList2nd)
    {
        foreach (var obj in objList2nd)
        {
            var mo = obj as MemObject;
            if (mo != null)
            {
                if (mo._thing is NativeUnityEngineObject)
                {
                    var n = mo._thing as NativeUnityEngineObject;
                    _nativeObjects.Add(n.instanceID, mo);
                }
                else if (mo._thing is ManagedObject)
                {
                    var m = mo._thing as ManagedObject;
                    _managedObjects.Add(m.address, mo);
                }
                else if (mo._thing is GCHandle)
                {
                    _gchandles.Add(mo);
                }
                else if (mo._thing is StaticFields)
                {
                    _staticFields.Add(mo);
                }
            }
        }
    }

    public static List<object> Diff(List<object> obj1st, List<object> obj2nd)
    {
        MemObjectInfoSet set1st = new MemObjectInfoSet(obj1st);
        MemObjectInfoSet set2nd = new MemObjectInfoSet(obj2nd);

        List<object> nativeOnes = DiffNative(set1st._nativeObjects, set2nd._nativeObjects);
        List<object> managedOnes = DiffManaged(set1st._managedObjects, set2nd._managedObjects);

        List<object> ret = new List<object>();
        ret.AddRange(nativeOnes);
        ret.AddRange(managedOnes);
        return ret;
    }

    private static List<object> DiffNative(Dictionary<int, MemObject> native1st, Dictionary<int, MemObject> native2nd)
    {
        HashSet<int> both = Intersect(native1st, native2nd);

        List<object> ret = new List<object>();

        foreach (var p in native1st)
        {
            if (!both.Contains(p.Key))
            {
                MarkStatus(p.Value, eDiffStatus.Removed);
                ret.Add(p.Value);
            }
        }

        foreach (var p in native2nd)
        {
            if (!both.Contains(p.Key))
            {
                MarkStatus(p.Value, eDiffStatus.Added);
                ret.Add(p.Value);
            }
        }

        foreach (int i in both)
        {
            NativeUnityEngineObject obj1 = native1st[i]._thing as NativeUnityEngineObject;
            NativeUnityEngineObject obj2 = native2nd[i]._thing as NativeUnityEngineObject;

            MemObject mo = native2nd[i];
            if (obj1.size == obj2.size)
            {
                MarkStatus(mo, eDiffStatus.Unchanged);
            }
            else if (obj1.size > obj2.size)
            {
                MarkStatus(mo, eDiffStatus.Decreased);
                ret.Add(mo);
            }
            else 
            {
                MarkStatus(mo, eDiffStatus.Increased);
                ret.Add(mo);
            }
        }

        return ret;
    }

    private static List<object> DiffManaged(Dictionary<UInt64, MemObject> managed1st, Dictionary<UInt64, MemObject> managed2nd)
    {
        HashSet<UInt64> both = Intersect(managed1st, managed2nd);

        List<object> ret = new List<object>();

        foreach (var p in managed1st)
        {
            if (!both.Contains(p.Key))
            {
                MarkStatus(p.Value, eDiffStatus.Removed);
                ret.Add(p.Value);
            }
        }

        foreach (var p in managed2nd)
        {
            if (!both.Contains(p.Key))
            {
                MarkStatus(p.Value, eDiffStatus.Added);
                ret.Add(p.Value);
            }
        }

        foreach (UInt64 i in both)
        {
            ManagedObject obj1 = managed1st[i]._thing as ManagedObject;
            ManagedObject obj2 = managed2nd[i]._thing as ManagedObject;

            MemObject mo = managed2nd[i];
            if (obj1.size == obj2.size)
            {
                MarkStatus(mo, eDiffStatus.Unchanged);
            }
            else if (obj1.size > obj2.size)
            {
                MarkStatus(mo, eDiffStatus.Decreased);
                ret.Add(mo);
            }
            else
            {
                MarkStatus(mo, eDiffStatus.Increased);
                ret.Add(mo);
            }
        }

        return ret;
    }

    private static HashSet<T> Intersect<T>(Dictionary<T, MemObject> d1, Dictionary<T, MemObject> d2)
    {
        HashSet<T> both = new HashSet<T>();
        foreach (var k in d1.Keys)
        {
            if (d2.ContainsKey(k))
                both.Add(k);
        }
        return both;
    }

    private static void MarkStatus(MemObject mo, eDiffStatus status)
    {
        mo.InstanceName = Prefixes[(int)status] + mo.InstanceName;
    }

    Dictionary<int, MemObject> _nativeObjects = new Dictionary<int, MemObject>();
    Dictionary<UInt64, MemObject> _managedObjects = new Dictionary<ulong, MemObject>();
    List<MemObject> _gchandles = new List<MemObject>();
    List<MemObject> _staticFields = new List<MemObject>();
}
