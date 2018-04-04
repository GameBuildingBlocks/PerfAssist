using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public enum UIHighlightType
{
    HType_UpdateGeometry,
    HType_Invalidate,
}

public class UIHighlightWidget
{
    public string name;
    public Vector3[] screenPos;
    public float timeChanged;
    public UIHighlightType type;
}

public class UIDebugDraw : MonoBehaviour
{
    public static UIDebugDraw Instance = null;

    Texture m_texUpdateGeometry;
    Texture m_texUpdateAlpha;
    List<UIHighlightWidget> m_highlightedWidgets = new List<UIHighlightWidget>();

    Dictionary<string, int> m_widgetHighlightCount = new Dictionary<string, int>(100);

    // #gulu UITool.UIWorldToScreen() is a NGUI function
#if JX3M 
    public void HighlightWidget(string widgetName, Vector3[] widgetWorldCorners, UIHighlightType type)
    {
        if (!enabled)
            return;

        Vector3[] widgetScreenPos = new Vector3[widgetWorldCorners.Length];
        for (int k = 0; k < widgetWorldCorners.Length; ++k)
            widgetScreenPos[k] = UITool.UIWorldToScreen(widgetWorldCorners[k]);

        UIHighlightWidget widget = new UIHighlightWidget();
        widget.name = widgetName;
        widget.screenPos = widgetScreenPos;
        widget.timeChanged = Time.time;
        widget.type = type;
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
#endif

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
        m_texUpdateGeometry = texture;

        texture = new Texture2D(1, 1);
        c = Color.green;
        c.a = 0.2f;
        texture.SetPixel(0, 0, c);
        texture.Apply();
        m_texUpdateAlpha = texture;
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

            switch (m_highlightedWidgets[k].type)
            {
                case UIHighlightType.HType_UpdateGeometry:
                    GUI.DrawTexture(r, m_texUpdateGeometry, ScaleMode.StretchToFill);
                    break;
                case UIHighlightType.HType_Invalidate:
                    GUI.DrawTexture(r, m_texUpdateAlpha, ScaleMode.StretchToFill);
                    break;
            }

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
