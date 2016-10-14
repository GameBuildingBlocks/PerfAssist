using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public partial class TableView 
{
    private string GetSortMark()
    {
        return _descending ? " ▼" : " ▲";
    }

    private void DrawTitle(float width)
    {
        for (int i = 0; i < m_descArray.Count; i++)
        {
            var desc = m_descArray[i];

            Rect r = LabelRect(width, i, 0);
            GUI.Label(r, desc.TitleText + (_sortSlot == i ? GetSortMark() : ""), m_appr.Style_Title);
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                if (_sortSlot == i)
                {
                    _descending = !_descending;
                }
                else
                {
                    _sortSlot = i;
                }

                SortData();
                m_hostWindow.Repaint();
            }
        }
    }

    private void DrawLine(int pos, object entry, float width)
    {
        GUIStyle style = (pos % 2 != 0) ? m_appr.Style_Line : m_appr.Style_LineAlt;

        Rect r = new Rect(0, pos * m_appr.LineHeight, width, m_appr.LineHeight);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            m_selected = entry;
            style = m_appr.Style_Selected;

            if (OnLineSelected != null)
                OnLineSelected(m_selected);
        }

        for (int i = 0; i < m_descArray.Count; i++)
            DrawCell(pos, i, width, entry, style);
    }

    private void DrawCell(int pos, int slot, float width, object obj, GUIStyle style)
    {
        var desc = m_descArray[slot];
        style.alignment = desc.Alignment;
        GUI.Label(LabelRect(width, slot, pos), desc.FormatObject(obj), style);
    }

    private void SortData()
    {
        m_lines.Sort((s1, s2) =>
        {
            if (_sortSlot >= m_descArray.Count)
                return 0;

            return m_descArray[_sortSlot].Compare(s1, s2) * (_descending ? -1 : 1);
        });
    }

    private Rect LabelRect(float width, int slot, int pos)
    {
        float accumPercent = 0.0f;
        int count = Mathf.Min(slot, m_descArray.Count);
        for (int i = 0; i < count; i++)
        {
            accumPercent += m_descArray[i].WidthInPercent;
        }
        return new Rect(width * accumPercent, pos * m_appr.LineHeight, width * m_descArray[slot].WidthInPercent, m_appr.LineHeight);
    }

    Type m_itemType = null;
    EditorWindow m_hostWindow = null;
    List<object> m_lines = new List<object>();
    object m_selected = null;
    Vector2 _scrollPos = Vector2.zero;

    List<TableViewColDesc> m_descArray = new List<TableViewColDesc>();
    TableViewAppr m_appr = new TableViewAppr();

    int _sortSlot = 0;
    bool _descending = true;
}
