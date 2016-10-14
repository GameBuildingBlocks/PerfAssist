using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public delegate void LineSelectionHandler(object selected);

public partial class TableView : IDisposable 
{
    public event LineSelectionHandler OnLineSelected;

    public TableViewAppr Appearance { get { return m_appr; } }

    public TableView(EditorWindow hostWindow, Type itemType)
    {
        m_hostWindow = hostWindow;
        m_itemType = itemType;
    }

    public void Dispose()
    {

    }

    public void ClearColumns()
    {
        m_descArray.Clear();
    }

    public bool AddColumn(string colDataPropertyName, string colTitleText, float widthByPercent, TextAnchor alignment = TextAnchor.MiddleCenter, string fmt = "")
    {
        TableViewColDesc desc = new TableViewColDesc();
        desc.PropertyName = colDataPropertyName;
        desc.TitleText = colTitleText;
        desc.Alignment = alignment;
        desc.WidthInPercent = widthByPercent;
        desc.Format = string.IsNullOrEmpty(fmt) ? null : fmt;
        desc.fieldInfo = m_itemType.GetField(desc.PropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
        if (desc.fieldInfo == null)
            return false;

        m_descArray.Add(desc);
        return true;
    }

    public void RefreshData(List<object> entries)
    {
        if (entries == null)
            return;

        m_lines.Clear();
        m_lines.AddRange(entries);

        SortData();
    }

    public void Draw()
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
}
