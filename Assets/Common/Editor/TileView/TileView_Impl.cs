using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class TileView
{
    //private void DrawTitle(float width)
    //{
    //    for (int i = 0; i < m_descArray.Count; i++)
    //    {
    //        var desc = m_descArray[i];

    //        Rect r = LabelRect(width, i, 0);
    //        bool selected = _sortSlot == i;
    //        GUI.Label(r, desc.TitleText + (selected ? _appearance.GetSortMark(_descending) : ""), _appearance.GetTitleStyle(selected));
    //        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
    //        {
    //            if (_sortSlot == i)
    //            {
    //                _descending = !_descending;
    //            }
    //            else
    //            {
    //                _sortSlot = i;
    //            }

    //            //SortData();
    //            m_hostWindow.Repaint();
    //        }
    //    }
    //}

    private void DrawLine(int startY, int firstObjIndex, float width, float lineHeight)
    {
        GUIStyle style = new GUIStyle(((startY / (int)lineHeight) % 2 != 0) ? _appearance.Style_Line : _appearance.Style_LineAlt);

        for (int i = 0; i < ColumnCount; i++)
        {
            if (firstObjIndex + i > m_objects.Count - 1)
                break;

            DrawLineCol(startY, i, width, firstObjIndex + i, style, lineHeight);
        }
    }

    private void DrawLineCol(int startY, int col, float width, int objIndex, GUIStyle style, float lineHeight)
    {
        object obj = m_objects[objIndex];

        float cellWidth = width / ColumnCount;
        var rect = new Rect(cellWidth * col, startY, cellWidth, lineHeight);

        if (rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.MouseDown && m_selectedIndex != objIndex)
            {
                m_selectedIndex = objIndex;
                if (OnSelected != null)
                    OnSelected(obj, objIndex);
            }
            m_hostWindow.Repaint();
        }

        TextureInfoItem item = obj as TextureInfoItem;
        if (item != null)
        {
            GUI.DrawTexture(rect, item.TextureObject);

            if (m_selectedIndex == objIndex /*&& m_selected == obj*/)
            {
                style = _appearance.Style_SelectedCell;
            }

            Rect textRect = rect;
            textRect.y = rect.y + rect.height - 20;
            textRect.height = 20;
            style.alignment = TextAnchor.LowerLeft;

            string text = string.Format("{0}. {1} ({2}x{3})",
                objIndex, item.TextureName, item.TextureWidth, item.TextureHeight);

            GUI.Label(textRect, new GUIContent(text, text), style);
        }
    }

    //private void SortData()
    //{
    //    m_objects.Sort((s1, s2) =>
    //    {
    //        if (_sortSlot >= m_descArray.Count)
    //            return 0;

    //        return m_descArray[_sortSlot].Compare(s1, s2) * (_descending ? -1 : 1);
    //    });
    //}

    TileViewAppr _appearance = new TileViewAppr();

    Vector2 _scrollPos = Vector2.zero;

    bool _descending = true;
    public bool Descending
    {
        get { return _descending; }
        set
        {
            _descending = value;
        }
    }
    EditorWindow m_hostWindow = null;
    List<object> m_objects = new List<object>();

    object m_selected = null;
    int m_selectedIndex = -1;

    //Dictionary<object, Color> m_specialTextColors;
}
