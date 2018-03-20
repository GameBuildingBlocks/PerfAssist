#if !TENCENT_CHANNEL
using CodeStage.AdvancedFPSCounter;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class UIPanelData
{
    public double mElapsedTicks;
    public int mCalls;
    public int mRebuildCount;
    public int mDrawCallNum;
}

public class UITimingDict
{
    public UITimingDict(string name)
    {
        m_name = name;
    }

    public void StartTiming()
    {
        m_stopwatch.Reset();
        m_stopwatch.Start();
    }

    public void StopTiming(string name)
    {
        m_stopwatch.Stop();
        double ms = (double)m_stopwatch.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        if (m_elapsedTicks.ContainsKey(name))
        {
            m_elapsedTicks[name] += ms;
        }
        else
        {
            m_elapsedTicks.Add(name, ms);
        }
    }

    const string Indent = "    ";

    public string PrintDict(ref Dictionary<string, double> accumulated, int count = -1)
    {
        List<KeyValuePair<string, double>> l = m_elapsedTicks.ToList();
        l.Sort(
            delegate (KeyValuePair<string, double> pair1, KeyValuePair<string, double> pair2)
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
#if !TENCENT_CHANNEL
            builder.AppendFormat("{0}{1,-40} \t{2:0.00} \t{3:0.00}\n", Indent, p.Key, p.Value, p.Value / (double)AFPSCounter.Instance.fpsCounter.newValue);
#endif
            string concatName = string.Format("{0}:{1}", m_name, p.Key);
            if (accumulated.ContainsKey(concatName))
            {
                accumulated[concatName] += p.Value;
            }
            else
            {
                accumulated.Add(concatName, p.Value);
            }
        }
        return builder.ToString();
    }

    string m_name;

    Stopwatch m_stopwatch = Stopwatch.StartNew();
    Dictionary<string, double> m_elapsedTicks = new Dictionary<string, double>(200);
}

public class UIDebugStats : MonoBehaviour
{
    public static UIDebugStats Instance = null;

    Texture m_debugTexture;

    void Start ()
    {
        Texture2D texture = new Texture2D(1, 1);
        Color c = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        texture.SetPixel(0, 0, c);
        texture.Apply();
        m_debugTexture = texture;
    }

    public string PrintDictDouble()
    {
        List<KeyValuePair<string, UIPanelData>> l = m_elapsedTicks.ToList();
        l.Sort(
            delegate (KeyValuePair<string, UIPanelData> pair1, KeyValuePair<string, UIPanelData> pair2)
            {
                return Math.Sign(pair2.Value.mElapsedTicks - pair1.Value.mElapsedTicks);
            }
        );

        StringBuilder builder = new StringBuilder();
        foreach (var p in l)
        {
#if !TENCENT_CHANNEL
            builder.AppendFormat("{0, -25} \t{1:0.00} \t{2:0.00} \t{3}/{4} \t{5}\n", p.Key, p.Value.mElapsedTicks, p.Value.mElapsedTicks / (double)AFPSCounter.Instance.fpsCounter.newValue, p.Value.mRebuildCount, p.Value.mCalls, p.Value.mDrawCallNum / p.Value.mCalls);
#endif
            if (m_accumulated.ContainsKey(p.Key))
            {
                m_accumulated[p.Key] += p.Value.mElapsedTicks;
            }
            else
            {
                m_accumulated.Add(p.Key, p.Value.mElapsedTicks);
            }

            UITimingDict dict = null;
            if (m_widgetTicks.TryGetValue(p.Key, out dict))
            {
                builder.AppendFormat("{0}\n", dict.PrintDict(ref m_accumulated, 5));
            }
        }
        return builder.ToString();
    }

    string m_debugInfo;

    float m_lastUpdateTime = 0.0f;
	void Update ()
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

        if (Time.time - m_lastUpdateTime >= 1.0f)
		{
            m_debugInfo = PrintDictDouble();
            m_elapsedTicks.Clear();
            m_widgetTicks.Clear();

            m_lastUpdateTime = Time.time;
		}
	}

    private GUIStyle guiStyle = new GUIStyle();

    void OnGUI()
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

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

    int m_startFrame = -1;
    public void StartStats()
    {
        m_accumulated.Clear();
        m_lastUpdateTime = Time.time;
        m_startFrame = Time.frameCount;
    }

    public void StopStats()
    {
        List<KeyValuePair<string, double>> sortBuf = m_accumulated.ToList();
        sortBuf.Sort(
            delegate (KeyValuePair<string, double> pair1,
            KeyValuePair<string, double> pair2)
            {
                return pair2.Value.CompareTo(pair1.Value);
            }
        );

        int frameRecorded = Time.frameCount - m_startFrame;
        List<string> content = new List<string>();
        content.Add(string.Format("--- {0} frames ---", frameRecorded));
        foreach (var p in sortBuf)
        {
            content.Add(string.Format("{0} \t{1:0.00} \t{2:0.00}", p.Key, p.Value, p.Value / (double)frameRecorded));
        }
        string file = Path.Combine(Application.persistentDataPath, string.Format("TestTools/ui_stats_panels_{0}_{1}.log", SysUtil.FormatDateAsFileNameString(DateTime.Now), SysUtil.FormatTimeAsFileNameString(DateTime.Now)));
        System.IO.File.WriteAllLines(file, content.ToArray());
    }

    public void StartPanelUpdate()
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

        m_panelSW.Reset();
        m_panelSW.Start();
    }

    public void StopPanelUpdate(string panelName, bool bRebuild, int drawCallNum)
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

        m_panelSW.Stop();
        double ms = (double)m_panelSW.ElapsedTicks / (double)TimeSpan.TicksPerMillisecond;
        if (m_elapsedTicks.ContainsKey(panelName))
        {
            UIPanelData data = m_elapsedTicks[panelName];
            data.mElapsedTicks += ms;
            ++data.mCalls;
            data.mRebuildCount += bRebuild ? 1 : 0;
            data.mDrawCallNum += drawCallNum;
        } 
        else
        {
            m_elapsedTicks.Add(panelName, new UIPanelData
            {
                mElapsedTicks = ms,
                mCalls = 1,
                mRebuildCount = bRebuild ? 1 : 0,
                mDrawCallNum = drawCallNum
            });
        }
    }

    public void StartPanelWidget(string panelName)
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

        UITimingDict td = null;
        if (!m_widgetTicks.TryGetValue(panelName, out td))
        {
            td = new UITimingDict(panelName);
            m_widgetTicks.Add(panelName, td);
        }

        td.StartTiming();
    }

    public void StopPanelWidget(string panelName, string widgetName)
    {
        if (!GlobalSwitches.UIDebuggingPanels)
            return;

        UITimingDict td = null;
        if (!m_widgetTicks.TryGetValue(panelName, out td))
            return;

        td.StopTiming(widgetName);
    }

    Stopwatch m_panelSW = Stopwatch.StartNew();
    Dictionary<string, UIPanelData> m_elapsedTicks = new Dictionary<string, UIPanelData>(200);
    Dictionary<string, UITimingDict> m_widgetTicks = new Dictionary<string, UITimingDict>(200);

    Dictionary<string, double> m_accumulated = new Dictionary<string, double>(200);
}
