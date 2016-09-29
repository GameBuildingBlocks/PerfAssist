using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class CoTableEntry
{
    public string Name = "Foo";
    public float TimeTotal = 0.0f;
    public float TimeAvg = 0.0f;
    public float TimeMax = 0.0f;
    public float TimePicked = 0.0f;
}

public class Panel_CoTable 
{
    GUIStyle NameLabel;
    GUIStyle NameLabelDark;
    GUIStyle Selected;

    public static Panel_CoTable Instance = new Panel_CoTable();

    public Panel_CoTable()
    {
        for (int i = 0; i < 200; i++)
        {
            m_items.Add(new CoTableEntry());
        }

        NameLabel = new GUIStyle(EditorStyles.whiteLabel);
        NameLabel.alignment = TextAnchor.MiddleLeft;
        NameLabel.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.1f));
        NameLabel.normal.textColor = Color.white;

        NameLabelDark = new GUIStyle(EditorStyles.whiteLabel);
        NameLabelDark.alignment = TextAnchor.MiddleLeft;
        NameLabelDark.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        NameLabelDark.normal.textColor = Color.white;

        Selected = new GUIStyle(EditorStyles.whiteLabel);
        Selected.alignment = TextAnchor.MiddleLeft;
        Selected.normal.background = GuiUtil.getColorTexture(new Color(0.2f, 0.2f, 0.6f, 0.3f));
        Selected.normal.textColor = Color.white;
    }

    private CoTableEntry m_selected = null;
    private List<CoTableEntry> m_items = new List<CoTableEntry>();

    static float _nameWidth = 190.0f;
    static float _numWidth = 50;
    static float _lineHeight = 25;

    private void DrawLine(int pos, CoTableEntry entry, float width)
    {
        Rect r = new Rect(0, pos * _lineHeight, width, _lineHeight);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            m_selected = entry;
        }

        GUIStyle style = (pos % 2 != 0) ? NameLabel : NameLabelDark;
        if (m_selected == entry)
        {
            style = Selected;
        }

        GUI.Label(new Rect(0, pos * _lineHeight, _nameWidth, _lineHeight), entry.Name + ((m_selected == entry) ? "*" : ""), style);
        GUI.Label(new Rect(_nameWidth, pos * _lineHeight, _numWidth, _lineHeight), entry.TimeTotal.ToString("0.000"), style);
        GUI.Label(new Rect(_nameWidth + _numWidth, pos * _lineHeight, _numWidth, _lineHeight), entry.TimeAvg.ToString("0.000"), style);
        GUI.Label(new Rect(_nameWidth + _numWidth * 2, pos * _lineHeight, _numWidth, _lineHeight), entry.TimeMax.ToString("0.000"), style);
        GUI.Label(new Rect(_nameWidth + _numWidth * 3, pos * _lineHeight, width - (_nameWidth + _numWidth * 3), _lineHeight), entry.TimePicked.ToString("0.000"), style);
    }

    public void DrawTable()
    {
        GUIStyle s = new GUIStyle();
        s.fixedHeight = _lineHeight * m_items.Count;
        s.stretchWidth = true;
        Rect r = EditorGUILayout.BeginVertical(s);

        // !!! this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
        GuiUtil.DrawLabel("", NameLabel);

        for (int i = 0; i < m_items.Count; i++)
        {
            DrawLine(i, m_items[i], r.width);
        }
        EditorGUILayout.EndVertical();
    }
}
