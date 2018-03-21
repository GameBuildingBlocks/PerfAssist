using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class UIHighlightWidget
{
    public string name;
    public Vector3[] screenPos;
    public float timeChanged;
}

public class UIDebugDraw : MonoBehaviour
{
    public static UIDebugDraw Instance = null;

    Texture m_debugTexture;
    List<UIHighlightWidget> m_highlightedWidgets = new List<UIHighlightWidget>();

    Dictionary<string, int> m_widgetHighlightCount = new Dictionary<string, int>(100);

    public void HighlightWidget(string widgetName, Vector3[] widgetScreenPos)
    {
        if (!enabled)
            return;

        UIHighlightWidget widget = new UIHighlightWidget();
        widget.name = widgetName;
        widget.screenPos = widgetScreenPos;
        widget.timeChanged = Time.time;
        m_highlightedWidgets.Add(widget);

        if (m_widgetHighlightCount.ContainsKey(widgetName))
        {
            m_widgetHighlightCount[widgetName]++;
        }
        else
        {
            m_widgetHighlightCount.Add(widgetName, 1);
        }
    }

    public void StartStats()
    {
        if (enabled)
            return;

        enabled = true;
        m_widgetHighlightCount.Clear();
    }

    public void StopStats()
    {
        if (!enabled)
            return;

        enabled = false;
        List<KeyValuePair<string, int>> sortBuf = m_widgetHighlightCount.ToList();
        sortBuf.Sort(
            delegate (KeyValuePair<string, int> pair1,
            KeyValuePair<string, int> pair2)
            {
                return pair2.Value.CompareTo(pair1.Value);
            }
        );

        List<string> content = new List<string>();
        foreach (var p in sortBuf)
        {
            content.Add(string.Format("{0} {1}", p.Key, p.Value));
        }
        string file = Path.Combine(Application.persistentDataPath, string.Format("TestTools/ui_stats_{0}_{1}.log", SysUtil.FormatDateAsFileNameString(DateTime.Now), SysUtil.FormatTimeAsFileNameString(DateTime.Now))) ;
        System.IO.File.WriteAllLines(file, content.ToArray());
    }

    // Use this for initialization
    void Start()
    {
        Texture2D texture = new Texture2D(1, 1);
        Color c = Color.magenta;
        c.a = 0.2f;
        texture.SetPixel(0, 0, c);
        texture.Apply();
        m_debugTexture = texture;
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + new Vector3(100, 0, 0), Color.red);
        //Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + new Vector3(100, 100, -50), Color.red);
        //Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + new Vector3(0, 100, 0), Color.red);
    }

    void OnGUI()
    {
        if (!enabled)
            return;

        var savedColor = GUI.color;
        var texColor = GUI.color;
        for (int k = 0; k < m_highlightedWidgets.Count; ++k)
        {
            texColor.a = 1.0f - (Time.time - m_highlightedWidgets[k].timeChanged) * 2;
            GUI.color = texColor;

            Vector3[] widgetScreenPos = m_highlightedWidgets[k].screenPos;
            float height = widgetScreenPos[2].y - widgetScreenPos[0].y;
            var r = new Rect(
                widgetScreenPos[0].x,
                Screen.height - widgetScreenPos[0].y - height,
                widgetScreenPos[2].x - widgetScreenPos[0].x,
                height);

            GUI.DrawTexture(r, m_debugTexture, ScaleMode.StretchToFill);

            r.width = Math.Max(r.width, 200); // make sure the space is enough for text dispalaying
            r.height = Math.Max(r.height, 50);
            GUI.Label(r, m_highlightedWidgets[k].name);
        }

        GUI.color = savedColor;

        m_highlightedWidgets.RemoveAll((w) => { return Time.time - w.timeChanged > 0.5f; });
    }

    void OnDrawGizmos()
    {
        //if (m_debugTexture != null)
        //{
        //    Gizmos.DrawGUITexture(new Rect(10, 10, 20, 20), m_debugTexture);
        //}
    }
}
