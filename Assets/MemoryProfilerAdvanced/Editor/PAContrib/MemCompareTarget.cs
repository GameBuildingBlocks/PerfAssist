using UnityEngine;
using System.Collections;
using System;
using MemoryProfilerWindow;
using System.Collections.Generic;

public class MemCompareTarget
{
    public static MemCompareTarget Instance = new MemCompareTarget();

    [NonSerialized]
    UnityEditor.MemoryProfiler.PackedMemorySnapshot _snapshot;
    [SerializeField]
    PackedCrawlerData _packedCrawled;
    [NonSerialized]
    CrawledMemorySnapshot _unpackedCrawl;

    public void SetCompareTarget(UnityEditor.MemoryProfiler.PackedMemorySnapshot snapshot)
    {
        _snapshot = snapshot;
        _packedCrawled = new Crawler().Crawl(_snapshot);
        _unpackedCrawl = CrawlDataUnpacker.Unpack(_packedCrawled);
    }

    public HashSet<int> GetNewlyAdded(CrawledMemorySnapshot latestCrawl)
    {
        HashSet<int> compTargets = new HashSet<int>();
        foreach (var item in _unpackedCrawl.allObjects)
        {
            var n = item as NativeUnityEngineObject;
            if (n != null)
            {
                compTargets.Add(n.instanceID);
            }
        }

        HashSet<int> newlyAdded = new HashSet<int>();
        foreach (var item in latestCrawl.allObjects)
        {
            var n = item as NativeUnityEngineObject;
            if (n != null && !compTargets.Contains(n.instanceID))
            {
                newlyAdded.Add(n.instanceID);
            }
        }
        return newlyAdded;
    }

    public CrawledMemorySnapshot UnpackedTargetSnapshot { get { return _unpackedCrawl; } }
}
