using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;


public class CoTableEntry
{
    public int SeqID = -1;
    public string Name = "Foo";
    public int ExecSelectedCount = 0;
    public float ExecSelectedTime = 0.0f;
    public int ExecAccumCount = 0;
    public float ExecAccumTime = 0.0f;
}

public delegate void LineSelectionHandler(int coSeqID);

public class TableView : IDisposable 
{
    private LineSelectionHandler _onLineSelected;
    public event LineSelectionHandler OnLineSelected
    {
        add
        {
            _onLineSelected -= value;
            _onLineSelected += value;
        }
        remove
        {
            _onLineSelected -= value;
        }
    }

    public TableView(EditorWindow hostWindow)
    {
        m_hostWindow = hostWindow;
    }

    public void Dispose()
    {

    }

    public void ClearColumns()
    {
        m_descArray.Clear();
    }

    public void AddColumn(string colDataPropertyName, string colTitleText, float widthByPercent, string fmt = "")
    {
        TableViewColDesc desc = new TableViewColDesc();
        desc.PropertyName = colDataPropertyName;
        desc.TitleText = colTitleText;
        desc.WidthInPercent = widthByPercent;
        desc.Format = string.IsNullOrEmpty(fmt) ? null : fmt;
        AddColumn(desc);
    }

    public void AddColumn(TableViewColDesc desc)
    {
        m_descArray.Add(desc);
    }

    private EditorWindow m_hostWindow = null;
    private CoTableEntry m_selected = null;
    private List<CoTableEntry> m_lines = new List<CoTableEntry>();

    public List<TableViewColDesc> DescArray { get { return m_descArray; } }
    List<TableViewColDesc> m_descArray = new List<TableViewColDesc>();

    public TableViewAppr Appearance { get { return m_appr; } }
    private TableViewAppr m_appr = new TableViewAppr();

    static float[] _starts = new float[5] { 0.0f, 0.58f, 0.64f, 0.74f, 0.86f };
    static float[] _ratios = new float[5] { 0.58f, 0.06f, 0.1f, 0.12f, 0.14f };

    private Rect LabelRect(float width, int slot, int pos)
    {
        return new Rect(width * _starts[slot], pos * m_appr.LineHeight, width * _ratios[slot], m_appr.LineHeight);
    }

    Dictionary<string, int> TitleSlots = new Dictionary<string, int>()
    {
        { "Name", 0 },
        { "Cnt", 1 },
        { "Time", 2 },
        { "Cnt_Sum", 3 },
        { "Time_Sum", 4 },
    };

    int _sortSlot = 4;

    private void DrawTitle(float width)
    {
        foreach (var item in TitleSlots)
        {
            Rect r = LabelRect(width, item.Value, 0);
            GUI.Label(r, item.Key + (_sortSlot == item.Value ? " ▼" : ""), m_appr.Style_Title);
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                _sortSlot = item.Value;

                Sort();
                m_hostWindow.Repaint();
            }
        }
    }

    private void DrawLine(int pos, CoTableEntry entry, float width)
    {
        Rect r = new Rect(0, pos * m_appr.LineHeight, width, m_appr.LineHeight);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            m_selected = entry;

            if (_onLineSelected != null)
                _onLineSelected(m_selected.SeqID);
        }

        GUIStyle style = new GUIStyle((pos % 2 != 0) ? m_appr.Style_Line : m_appr.Style_LineAlt);
        if (m_selected == entry)
        {
            style = m_appr.Style_Selected;
        }

        style.alignment = TextAnchor.MiddleLeft;
        GUI.Label(LabelRect(width, 0, pos), entry.Name + ((m_selected == entry) ? "*" : ""), style);

        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(LabelRect(width, 1, pos), entry.ExecSelectedCount.ToString(), style);
        GUI.Label(LabelRect(width, 2, pos), entry.ExecSelectedTime.ToString("0.000"), style);
        GUI.Label(LabelRect(width, 3, pos), entry.ExecAccumCount.ToString(), style);
        GUI.Label(LabelRect(width, 4, pos), entry.ExecAccumTime.ToString("0.000"), style);
    }

    public void DrawTable()
    {
        GUIStyle s = new GUIStyle();
        s.fixedHeight = m_appr.LineHeight * m_lines.Count;
        s.stretchWidth = true;
        Rect r = EditorGUILayout.BeginVertical(s);

        // !!! this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
        GuiUtil.DrawLabel("", m_appr.Style_Line);

        DrawTitle(r.width);

        for (int i = 0; i < m_lines.Count; i++)
        {
            DrawLine(i + 1, m_lines[i], r.width);
        }

        EditorGUILayout.EndVertical();
    }

    public void RefreshEntries(List<CoTableEntry> entries)
    {
        if (entries == null)
            return;

        m_lines.Clear();
        m_lines.AddRange(entries);

        Sort();
    }

    private void Sort()
    {
        m_lines.Sort((s1, s2) =>
        {
            switch (_sortSlot)
            {
                case 1:
                    return -1 * s1.ExecSelectedCount.CompareTo(s2.ExecSelectedCount);
                case 2:
                    return -1 * s1.ExecSelectedTime.CompareTo(s2.ExecSelectedTime);
                case 3:
                    return -1 * s1.ExecAccumCount.CompareTo(s2.ExecAccumCount);
                case 4:
                    return -1 * s1.ExecAccumTime.CompareTo(s2.ExecAccumTime);

                case 0:
                default:
                    break;
            }
            return s1.Name.CompareTo(s2.Name);
        });
    }
}
