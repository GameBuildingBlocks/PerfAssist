using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;

public partial class TableView 
{
    private void DrawTitle(float width)
    {
        for (int i = 0; i < m_descArray.Count; i++)
        {
            var desc = m_descArray[i];

            Rect r = LabelRect(width, i, 0);
            bool selected = _sortSlot == i;
            GUI.Label(r, desc.TitleText + (selected ? _appearance.GetSortMark(_descending) : ""), _appearance.GetTitleStyle(selected));
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

    private void DrawLine(int pos, object obj, float width)
    {
        Rect r = new Rect(0, pos * _appearance.LineHeight, width, _appearance.LineHeight);
        bool selectionHappens = Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition);

        GUIStyle style = new GUIStyle((pos % 2 != 0) ? _appearance.Style_Line : _appearance.Style_LineAlt);
        if (selectionHappens)
        {
            m_selected = obj;
        }

        // note that the 'selected-style' assignment below should be isolated from the if-conditional statement above
        // since the above if is a one-time event, on the contrary, the 'selected-style' assignment below should be done every time in the drawing process
        if (m_selected == obj)
        {
            style = _appearance.Style_Selected;
        }
        else
        {
            Color specialColor;
            if (m_specialTextColors != null &&
                m_specialTextColors.TryGetValue(obj, out specialColor))
            {
                style.normal.textColor = specialColor;
            }
        }

        for (int i = 0; i < m_descArray.Count; i++)
            DrawLineCol(pos, i, width, obj, style, selectionHappens);
    }

    private void DrawLineCol(int pos, int col, float width, object obj, GUIStyle style, bool selectionHappens = false)
    {
        var rect = LabelRect(width, col, pos);

        if (selectionHappens && rect.Contains(Event.current.mousePosition))
        {
            m_selectedCol = col;
            if (OnSelected != null)
                OnSelected(obj, col);
            m_hostWindow.Repaint();
        }

        var desc = m_descArray[col];
        string strPos = pos.ToString();
        string text;
        if (col == 0)
            text = strPos + ". " + desc.FormatObject(obj);
        else
            text = desc.FormatObject(obj);

        // note that the 'selected-style' assignment below should be isolated from the if-conditional statement above
        // since the above if is a one-time event, on the contrary, the 'selected-style' assignment below should be done every time in the drawing process
        if (m_selectedCol == col && m_selected == obj)
        {
            style = _appearance.Style_SelectedCell;
        }

        style.alignment = desc.Alignment;
        GUI.Label(rect, new GUIContent(text, text), style);
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
        return new Rect(width * accumPercent, pos * _appearance.LineHeight, width * m_descArray[slot].WidthInPercent, _appearance.LineHeight);
    }

    List<TableViewColDesc> m_descArray = new List<TableViewColDesc>();
    TableViewAppr _appearance = new TableViewAppr();

    Vector2 _scrollPos = Vector2.zero;

    int _sortSlot = 0;
    bool _descending = true;

    Type m_itemType = null;
    EditorWindow m_hostWindow = null;
    List<object> m_lines = new List<object>();

    object m_selected = null;
    int m_selectedCol = -1;

    Dictionary<object, Color> m_specialTextColors;
}
