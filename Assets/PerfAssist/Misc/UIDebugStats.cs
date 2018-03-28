using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;


public class UIDebugVariables
{
    public static bool ShowWidgetStatsOnScreen = false;
}

public class UIDebugCache
{
    public static string GetName(int instID)
    {
        return s_nameLut.ContainsKey(instID) ? s_nameLut[instID] : "";
    }

    public static string GetParentName(int instID)
    {
        return s_parentNameLut.ContainsKey(instID) ? s_parentNameLut[instID] : "";
    }

    public static Dictionary<int, string> s_nameLut = new Dictionary<int, string>();
    public static Dictionary<int, string> s_parentNameLut = new Dictionary<int, string>();
}

public class UIPanelData
{
    public double mElapsedTicks;
    public int mCalls;
    public int mRebuildCount;
    public int mDrawCallNum;

    internal void Enlarge(UIPanelData value)
    {
        mElapsedTicks += value.mElapsedTicks;
        mCalls += value.mCalls;
        mRebuildCount += value.mRebuildCount;
        mDrawCallNum += value.mDrawCallNum;
    }
}

public class UITimingDict
{
    public void StartTiming()
    {
        m_stopwatch.Reset();
        m_stopwatch.Start();
    }

    public double StopTiming(UnityEngine.Object w)
    {
        int instID = w.GetInstanceID();
        m_stopwatch.Stop();
        double ms = (double)m_stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        if (m_elapsedTicks.ContainsKey(instID))
        {
            m_elapsedTicks[instID] += ms;
        }
        else
        {
            m_elapsedTicks.Add(instID, ms);
        }

        if (!UIDebugCache.s_nameLut.ContainsKey(instID))
            UIDebugCache.s_nameLut.Add(instID, w.name);

        return ms;
    }

    const string Indent = "    ";

    public string PrintDict(int count = -1)
    {
        List<KeyValuePair<int, double>> l = m_elapsedTicks.ToList();
        l.Sort(
            delegate (KeyValuePair<int, double> pair1, KeyValuePair<int, double> pair2)
            {
                return Math.Sign(pair2.Value - pair1.Value);
            }
        );

        if (count > 0 && count < l.Count)
        {
            l.RemoveRange(count - 1, l.Count - count);
        }

        StringBuilder builder = new StringBuilder();
        foreach (var p in l)
        {
            builder.AppendFormat("{0}{1,-40} \t{2:0.00} \t{3:0.00}\n", Indent, UIDebugCache.GetName(p.Key), p.Value, p.Value / (double)(1.0f / Time.deltaTime));
        }
        return builder.ToString();
    }

    Stopwatch m_stopwatch = Stopwatch.StartNew();
    Dictionary<int, double> m_elapsedTicks = new Dictionary<int, double>(200);
}

public class UIDebugStats : MonoBehaviour
{
    public static UIDebugStats Instance = null;

    Texture m_debugTexture;

    void Start ()
    {
        Texture2D texture = new Texture2D(1, 1);
        Color c = new Color(0.2f, 0.2f, 0.2f, 0.4f);
        texture.SetPixel(0, 0, c);
        texture.Apply();
        m_debugTexture = texture;
    }

    public string PrintDictDouble()
    {
        List<KeyValuePair<int, UIPanelData>> l = m_elapsedTicks.ToList();
        l.Sort(
            delegate (KeyValuePair<int, UIPanelData> pair1, KeyValuePair<int, UIPanelData> pair2)
            {
                return Math.Sign(pair2.Value.mElapsedTicks - pair1.Value.mElapsedTicks);
            }
        );

        StringBuilder builder = new StringBuilder();
        foreach (var p in l)
        {
            builder.AppendFormat("{0, -30} \t{1:0.00} \t{2:0.00} \t{3}/{4} \t{5}\n", 
                UIDebugCache.GetName(p.Key), p.Value.mElapsedTicks, p.Value.mElapsedTicks / (double)(1.0f / Time.deltaTime), p.Value.mRebuildCount, p.Value.mCalls, p.Value.mDrawCallNum / p.Value.mCalls);

            if (UIDebugVariables.ShowWidgetStatsOnScreen)
            {
                UITimingDict dict = null;
                if (m_widgetTicks.TryGetValue(p.Key, out dict))
                {
                    builder.AppendFormat("{0}\n", dict.PrintDict(5));
                }
            }
        }
        return builder.ToString();
    }

    string m_debugInfo;

    float m_lastUpdateTime = 0.0f;
	void Update ()
    {
        if (Time.time - m_lastUpdateTime >= 1.0f)
		{
            foreach (var p in m_elapsedTicks)
            {
                if (m_accumulatedPanels.ContainsKey(p.Key))
                {
                    m_accumulatedPanels[p.Key].Enlarge(p.Value);
                }
                else
                {
                    m_accumulatedPanels.Add(p.Key, p.Value);
                }
            }

            m_debugInfo = PrintDictDouble();
            m_elapsedTicks.Clear();
            m_widgetTicks.Clear();

            m_lastUpdateTime = Time.time;
		}
	}

    private GUIStyle guiStyle = new GUIStyle();

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(m_debugInfo))
        {
            Rect r = new Rect(250, 60, Screen.width - 640, Screen.height - 120.0f);

            GUI.DrawTexture(r, m_debugTexture);

            guiStyle.fontSize = 20;
            //guiStyle.clipping = TextClipping.Clip;
            guiStyle.normal.textColor = Color.white;
            GUI.Label(r, m_debugInfo, guiStyle);
        }
    }

    float m_startTime = 0.0f;
    int m_startFrame = -1;
    public void StartStats()
    {
        if (enabled)
            return;

        enabled = true;
        m_accumulated.Clear();
        m_accumulatedPanels.Clear();
        m_lastUpdateTime = Time.time;
        m_startFrame = Time.frameCount;
        m_startTime = Time.time;
    }

    public void StopStats()
    {
        if (!enabled)
            return;

        enabled = false;

        float seconds = Time.time - m_startTime;

        List<string> content = new List<string>();
        int frameRecorded = Time.frameCount - m_startFrame;
        content.Add(string.Format("name \ttotalMS \tperFrameMS \trebuildCount \tupdateCount \tdrawcallCount/updateCount \t --- {0} frames ---", frameRecorded));

        {
            List<KeyValuePair<int, UIPanelData>> panelData = m_accumulatedPanels.ToList();
            panelData.Sort(
                delegate (KeyValuePair<int, UIPanelData> pair1,
                KeyValuePair<int, UIPanelData> pair2)
                {
                    return pair2.Value.mElapsedTicks.CompareTo(pair1.Value.mElapsedTicks);
                }
            );

            foreach (var p in panelData)
            {
                string name = UIDebugCache.GetName(p.Key);
                UIPanelData data = p.Value;
                content.Add(string.Format("{0}\t{1:0.00}\t{2:0.00}\t{3}\t{4}\t{5}",
                    name, data.mElapsedTicks, data.mElapsedTicks / (double)frameRecorded,
                    (int)(data.mRebuildCount / seconds), (int)(data.mCalls / seconds), data.mDrawCallNum / data.mCalls));
            }
        }

        List<KeyValuePair<int, double>> sortBuf = m_accumulated.ToList();
        sortBuf.Sort(
            delegate (KeyValuePair<int, double> pair1,
            KeyValuePair<int, double> pair2)
            {
                return pair2.Value.CompareTo(pair1.Value);
            }
        );

        foreach (var p in sortBuf)
        {
            string name = UIDebugCache.GetName(p.Key);
            string parentName = UIDebugCache.GetParentName(p.Key);
            if (!string.IsNullOrEmpty(parentName))
            {
                name = string.Format("{0}:{1}", parentName, name);
            }
            content.Add(string.Format("{0}\t{1:0.00}\t{2:0.00}", name, p.Value, p.Value / (double)frameRecorded));
        }
        string file = Path.Combine(Application.persistentDataPath, string.Format("TestTools/ui_stats_panels_{0}_{1}.log", SysUtil.FormatDateAsFileNameString(DateTime.Now), SysUtil.FormatTimeAsFileNameString(DateTime.Now)));
        System.IO.File.WriteAllLines(file, content.ToArray());
    }

    public void StartPanelUpdate()
    {
        if (!enabled)
            return;

        m_panelSW.Reset();
        m_panelSW.Start();
    }

    public void StopPanelUpdate(UnityEngine.Object panel, bool bRebuild, int drawCallNum)
    {
        if (!enabled)
            return;

        int instID = panel.GetInstanceID();
        m_panelSW.Stop();
        double ms = (double)m_panelSW.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        if (m_elapsedTicks.ContainsKey(instID))
        {
            UIPanelData data = m_elapsedTicks[instID];
            data.mElapsedTicks += ms;
            ++data.mCalls;
            data.mRebuildCount += bRebuild ? 1 : 0;
            data.mDrawCallNum += drawCallNum;
        } 
        else
        {
            m_elapsedTicks.Add(instID, new UIPanelData
            {
                mElapsedTicks = ms,
                mCalls = 1,
                mRebuildCount = bRebuild ? 1 : 0,
                mDrawCallNum = drawCallNum
            });

            if (!UIDebugCache.s_nameLut.ContainsKey(instID))
            {
                UIDebugCache.s_nameLut.Add(instID, panel.name);
            }
        }
    }

    public void StartPanelWidget(UnityEngine.Object p)
    {
        if (!enabled)
            return;

        int pInstID = p.GetInstanceID();
        UITimingDict td = null;
        if (!m_widgetTicks.TryGetValue(pInstID, out td))
        {
            td = new UITimingDict();
            m_widgetTicks.Add(pInstID, td);
        }

        td.StartTiming();
    }

    public void StopPanelWidget(UnityEngine.Object p, UnityEngine.Object w)
    {
        if (!enabled)
            return;

        int pInstID = p.GetInstanceID();
        int wInstID = w.GetInstanceID();
        UITimingDict td = null;
        if (!m_widgetTicks.TryGetValue(pInstID, out td))
            return;

        if (!UIDebugCache.s_parentNameLut.ContainsKey(w.GetInstanceID()))
            UIDebugCache.s_parentNameLut.Add(w.GetInstanceID(), p.name);

        double ms = td.StopTiming(w);

        if (m_accumulated.ContainsKey(wInstID))
        {
            m_accumulated[wInstID] += ms;
        }
        else
        {
            m_accumulated.Add(wInstID, ms);
        }
    }

    Stopwatch m_panelSW = Stopwatch.StartNew();
    Dictionary<int, UIPanelData> m_elapsedTicks = new Dictionary<int, UIPanelData>(200);
    Dictionary<int, UITimingDict> m_widgetTicks = new Dictionary<int, UITimingDict>(200);

    Dictionary<int, double> m_accumulated = new Dictionary<int, double>(200);
    Dictionary<int, UIPanelData> m_accumulatedPanels = new Dictionary<int, UIPanelData>(200);
}
