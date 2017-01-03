using UnityEngine;
using System.Collections;
using UnityEditor;

public class TableViewAppr 
{
    public float LineHeight 
    { 
        get { return _lineHeight; } 
        set { _lineHeight = value; } 
    }
    float _lineHeight = 25;

    public string GetSortMark(bool descending)
    {
        return descending ? " ▼" : " ▲";
    }

    public GUIStyle GetTitleStyle(bool selected)
    {
        if (_styleTitle == null || _titleOrdinary == null || _titleSelected ==null)
        {
            _styleTitle = new GUIStyle(EditorStyles.whiteBoldLabel);
            _styleTitle.alignment = TextAnchor.MiddleCenter;
            _titleOrdinary = PAEditorUtil.getColorTexture(PAEditorConst.TitleColor);
            _titleSelected = PAEditorUtil.getColorTexture(PAEditorConst.TitleColorSelected); 
        }

        _styleTitle.normal.background = selected ? _titleSelected : _titleOrdinary;
        _styleTitle.normal.textColor = selected ? Color.yellow : Color.white;
        return _styleTitle;
    }
    private GUIStyle _styleTitle;
    private Texture2D _titleOrdinary;
    private Texture2D _titleSelected;

    public GUIStyle Style_Line
    {
        get
        {
            if (_styleLine == null)
            {
                _styleLine = new GUIStyle(EditorStyles.whiteLabel);
                _styleLine.normal.background = PAEditorUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.1f));
                _styleLine.normal.textColor = Color.white;
            }
            return _styleLine;
        }
    }
    private GUIStyle _styleLine;

    public GUIStyle Style_LineAlt
    {
        get
        {
            if (_styleLineAlt == null)
            {
                _styleLineAlt = new GUIStyle(EditorStyles.whiteLabel);
                _styleLineAlt.normal.background = PAEditorUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
                _styleLineAlt.normal.textColor = Color.white;
            }
            return _styleLineAlt;
        }
    }
    private GUIStyle _styleLineAlt;

    public GUIStyle Style_Selected
    {
        get
        {
            if (_styleSelected == null)
            {
                _styleSelected = new GUIStyle(EditorStyles.whiteLabel);
                _styleSelected.normal.background = PAEditorUtil.getColorTexture(PAEditorConst.SelectionColor);
                _styleSelected.normal.textColor = Color.white;
            }
            return _styleSelected;
        }
    }
    private GUIStyle _styleSelected;

    public GUIStyle Style_SelectedCell
    {
        get
        {
            if (_styleSelectedCell == null)
            {
                _styleSelectedCell = new GUIStyle(EditorStyles.whiteBoldLabel);
                _styleSelectedCell.normal.background = PAEditorUtil.getColorTexture(PAEditorConst.SelectionColorDark);
                _styleSelectedCell.normal.textColor = Color.yellow;
            }
            return _styleSelectedCell;
        }
    }
    private GUIStyle _styleSelectedCell;
}
