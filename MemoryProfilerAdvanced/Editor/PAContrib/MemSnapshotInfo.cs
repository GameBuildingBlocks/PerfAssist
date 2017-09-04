using UnityEngine;
using System.Collections;
using MemoryProfilerWindow;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;

public class MemSnapshotInfo
{
    public CrawledMemorySnapshot Unpacked { get { return _unpacked; } }
    private CrawledMemorySnapshot _unpacked = null;

    public int TotalCount { get { return _totalCount; } }
    public int _totalCount = 0;

    public int TotalSize { get { return _totalSize; } }
    public int _totalSize = 0;

    public bool AcceptSnapshot(PackedMemorySnapshot packed)
    {
        _totalCount = 0;
        _totalSize = 0;

        try
        {
            MemUtil.LoadSnapshotProgress(0.01f, "crawling");
            var crawled = new Crawler().Crawl(packed);

            MemUtil.LoadSnapshotProgress(0.7f, "unpacking");
            _unpacked = CrawlDataUnpacker.Unpack(crawled);

            MemUtil.LoadSnapshotProgress(0.9f, "populating");
            foreach (var thing in _unpacked.allObjects)
            {
                _totalSize += thing.size;
                _totalCount++;
            }
            MemUtil.LoadSnapshotProgress(1.0f, "done");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            _unpacked = null;
            return false;
        }

        return true;
    }

}
