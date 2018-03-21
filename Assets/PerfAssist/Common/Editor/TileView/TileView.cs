using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;

public delegate void TileSelectionHandler(object selected, int tile);

public partial class TileView : IDisposable
{
    public event TileSelectionHandler OnSelected;

    public int ColumnCount = 4;

    public TileViewAppr Appearance { get { return _appearance; } }

    public TileView(EditorWindow hostWindow)
    {
        m_hostWindow = hostWindow;
    }

    public void Dispose()
    {

    }

    //public bool AddColumn(string colDataPropertyName, string colTitleText, float widthByPercent, TextAnchor alignment = TextAnchor.MiddleCenter, string fmt = "")
    //{
    //    TableViewColDesc desc = new TableViewColDesc();
    //    desc.PropertyName = colDataPropertyName;
    //    desc.TitleText = colTitleText;
    //    desc.Alignment = alignment;
    //    desc.WidthInPercent = widthByPercent;
    //    desc.Format = string.IsNullOrEmpty(fmt) ? null : fmt;
    //    desc.FieldInfo = m_itemType.GetField(desc.PropertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
    //    if (desc.FieldInfo == null)
    //    {
    //        Debug.LogWarningFormat("Field '{0}' accessing failed.", desc.PropertyName);
    //        return false;
    //    }

    //    m_descArray.Add(desc);
    //    return true;
    //}

    public void RefreshData(List<object> entries)
    {
        m_objects.Clear();

        if (entries != null && entries.Count > 0)
        {
            m_objects.AddRange(entries);

            //SortData();
        }
    }

    public void Draw(Rect area)
    {
        GUILayout.BeginArea(area);
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
        //Debug.LogFormat("scroll pos: {0:0.00}, {1:0.00}", _scrollPos.x, _scrollPos.y);
        {
            float lineHeight = area.width / ColumnCount;

            GUIStyle s = new GUIStyle();
            s.fixedHeight = lineHeight * (m_objects.Count / ColumnCount + 2);
            s.stretchWidth = true;
            Rect r = EditorGUILayout.BeginVertical(s);
            {
                // this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
                PAEditorUtil.DrawLabel("", _appearance.Style_Line);

                // these first/last calculations are for smart clipping 
                int firstLine = Mathf.Max((int)(_scrollPos.y / lineHeight) - 1, 0);
                int shownLineCount = (int)(area.height / lineHeight) + 3;
                int lastLine = Mathf.Min(firstLine + shownLineCount, m_objects.Count / ColumnCount + 2);

                for (int i = firstLine; i < lastLine; i++)
                {
                    if (i * ColumnCount > m_objects.Count - 1)
                        break;

                    DrawLine(i * (int)lineHeight, i * ColumnCount, r.width, lineHeight);
                }
            }
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    public void SetSortParams(int sortSlot, bool descending)
    {
        //_sortSlot = sortSlot;
        _descending = descending;
    }

    public void SetSelected(object obj)
    {
        m_selected = obj;

        if (OnSelected != null)
            OnSelected(obj, 0);
    }

    public object GetSelected()
    {
        return m_selected;
    }
}
