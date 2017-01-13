using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public class ResourceTrackerConst
{
    public static string shaderPropertyNameJsonPath =
    Path.Combine(Path.Combine(Application.persistentDataPath, "TestTools"), "ShaderPropertyNameRecord.json");
    public static char shaderPropertyNameJsonToken = '$';

    public static string FormatBytes(int bytes)
    {
        if (bytes < 0)
            return "error bytes";
        
        if (bytes<1024)
        {
            return bytes + "b";
        }
        else if (bytes < 1024 * 1024)
        {
            return bytes / 1024 + "kb";
        }
        else {
            return bytes / 1024 /1024 + "mb";
        }
    }
}



[Serializable]
public class Serialization<TKey, TValue> : ISerializationCallbackReceiver
{
    [SerializeField]
    List<TKey> keys;
    [SerializeField]
    List<TValue> values;

    Dictionary<TKey, TValue> target;
    public Dictionary<TKey, TValue> ToDictionary() { return target; }

    public Serialization(Dictionary<TKey, TValue> target)
    {
        this.target = target;
    }

    public void OnBeforeSerialize()
    {
        keys = new List<TKey>(target.Keys);
        values = new List<TValue>(target.Values);
    }

    public void OnAfterDeserialize()
    {
        var count = Math.Min(keys.Count, values.Count);
        target = new Dictionary<TKey, TValue>(count);
        for (var i = 0; i < count; ++i)
        {
            target.Add(keys[i], values[i]);
        }
    }
}
