

using System.Collections.Generic;
using UnityEngine;

public class PA_UIStatsConst
{
    public static float WriteInterval = 1.0f;
}

public class PA_UIFrameStats
{
    // WriteToBuffers stats
    public int _wtbCnt = 0;
    public int _wtbU1Cnt = 0;
    public int _wtbNormCnt = 0;
    public int _totalVertCount = 0;

    public void Clear()
    {
        _wtbCnt = 0;
        _wtbU1Cnt = 0;
        _wtbNormCnt = 0;
        _totalVertCount = 0;
    }
}

public class PA_UIStats
{
    // simply set `Instance` to be null (commen next line and uncomment the second line) to disable all stats
    public static PA_UIStats Instance = null;

    public void BeginFrame()
    {
        if (_cachedFrames.Count > 0)
        {
            _curFrame = _cachedFrames[_cachedFrames.Count - 1];
            _cachedFrames.RemoveAt(_cachedFrames.Count - 1);
        }
        else
        {
            _curFrame = new PA_UIFrameStats();
        }
    }

    public void EndFrame()
    {
        _lastSecFrames.Add(_curFrame);
        _curFrame = null;

        float passed = Time.realtimeSinceStartup - _lastWriteTime;
        if (passed >= PA_UIStatsConst.WriteInterval)
        {
            Debug.LogWarning(GenerateStatsInfo());

            // clear and cache all frames for reusing
            for (int i = 0; i < _lastSecFrames.Count; ++i)
                _lastSecFrames[i].Clear();
            _cachedFrames.AddRange(_lastSecFrames);
            _lastSecFrames.Clear();

            _lastWriteTime = Mathf.Floor(Time.realtimeSinceStartup);
        }
    }

    string GenerateStatsInfo()
    {
        _accum.Clear();
        _max.Clear();
        for (int i = 0; i < _lastSecFrames.Count; ++i)
        {
            _accum._wtbCnt += _lastSecFrames[i]._wtbCnt;
            _accum._wtbU1Cnt += _lastSecFrames[i]._wtbU1Cnt;
            _accum._wtbNormCnt += _lastSecFrames[i]._wtbNormCnt;
            _accum._totalVertCount += _lastSecFrames[i]._totalVertCount;

            _max._wtbCnt = Mathf.Max(_lastSecFrames[i]._wtbCnt, _max._wtbCnt);
            _max._wtbU1Cnt = Mathf.Max(_lastSecFrames[i]._wtbU1Cnt, _max._wtbU1Cnt);
            _max._wtbNormCnt = Mathf.Max(_lastSecFrames[i]._wtbNormCnt, _max._wtbNormCnt);
            _max._totalVertCount = Mathf.Max(_lastSecFrames[i]._totalVertCount, _max._totalVertCount);
        }

        string wtbAccum = string.Format("accum: <cnt: {0} u1: {1} norm: {2}, vert:{3}>",
            _accum._wtbCnt, _accum._wtbU1Cnt, _accum._wtbNormCnt, _accum._totalVertCount);
        string wtbMax = string.Format("max: <cnt: {0} u1: {1} norm: {2}, vert:{3}>",
            _max._wtbCnt, _max._wtbU1Cnt, _max._wtbNormCnt, _max._totalVertCount);

        string infoDrawCall = "";
        string infoBetterList = "";

#if JX3M
        infoDrawCall = string.Format("Draw: <active: {0} inactive: {1} inactive_proxy: {2}>",
            UIDrawCall.activeList.size,
            UIDrawCall.inactiveList.size,
            UIDrawCall.inactiveList_Proxy.size);

        infoBetterList = string.Format("BList: <active: {0} count: {1} accum: {2}>",
            0,//BetterListStats._activeTotalBytes,
            BetterListStats._allocMoreCount,
            BetterListStats._accumAllocBytes);
#endif

        return string.Format("{0} {1} -- {2} {3}", wtbAccum, wtbMax, infoDrawCall, infoBetterList);
    }

    // 当前正在被统计的帧
    public PA_UIFrameStats _curFrame = null;
    // 过去一秒钟的所有帧
    public List<PA_UIFrameStats> _lastSecFrames = new List<PA_UIFrameStats>(50);
    // 缓存用于重复利用的所有帧
    public List<PA_UIFrameStats> _cachedFrames = new List<PA_UIFrameStats>(50);

    PA_UIFrameStats _accum = new PA_UIFrameStats();
    PA_UIFrameStats _max = new PA_UIFrameStats();

    // internal state
    private float _lastWriteTime = 0.0f; // has been rounded to avoid accum error
}