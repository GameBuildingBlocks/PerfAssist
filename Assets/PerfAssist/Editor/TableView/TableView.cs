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

    public void Draw(Rect area)
    {
        GUILayout.BeginArea(area);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
        //Debug.LogFormat("scroll pos: {0:0.00}, {1:0.00}", _scrollPos.x, _scrollPos.y);
        {
            GUIStyle s = new GUIStyle();
            s.fixedHeight = m_appr.LineHeight * m_lines.Count;
            s.stretchWidth = true;
            Rect r = EditorGUILayout.BeginVertical(s);
            {
                // this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
                GuiUtil.DrawLabel("", m_appr.Style_Line);

                DrawTitle(r.width);

                // these first/last calculatings are for smart clipping 
                int firstLine = Mathf.Max((int)(_scrollPos.y / m_appr.LineHeight) - 1, 0);
                int shownLineCount = (int)(area.height / m_appr.LineHeight) + 2;
                int lastLine = Mathf.Min(firstLine + shownLineCount, m_lines.Count);

                for (int i = firstLine; i < lastLine; i++)
                {
                    DrawLine(i + 1, m_lines[i], r.width);
                }
            }
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
