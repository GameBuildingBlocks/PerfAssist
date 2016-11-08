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

    public TableViewAppr()
    {
        Style_Title = new GUIStyle(EditorStyles.whiteBoldLabel);
        Style_Title.alignment = TextAnchor.MiddleCenter;
        Style_Title.normal.background = PAEditorUtil.getColorTexture((Color)new Color32(38, 158, 111, 255));
        Style_Title.normal.textColor = Color.white;

        Style_Line = new GUIStyle(EditorStyles.whiteLabel);
        Style_Line.normal.background = PAEditorUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.1f));
        Style_Line.normal.textColor = Color.white;

        Style_LineAlt = new GUIStyle(EditorStyles.whiteLabel);
        Style_LineAlt.normal.background = PAEditorUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        Style_LineAlt.normal.textColor = Color.white;

        Style_Selected = new GUIStyle(EditorStyles.whiteLabel);
        Style_Selected.normal.background = PAEditorUtil.getColorTexture(PAEditorConst.SelectionColor);
        Style_Selected.normal.textColor = Color.white;

        Style_SelectedCell = new GUIStyle(EditorStyles.whiteLabel);
        Style_SelectedCell.normal.background = PAEditorUtil.getColorTexture(PAEditorConst.SelectionColorDark);
        Style_SelectedCell.normal.textColor = Color.yellow;
    }

    public string GetSortMark(bool descending)
    {
        return descending ? " ▼" : " ▲";
    }

    public GUIStyle Style_Title;
    public GUIStyle Style_Line;
    public GUIStyle Style_LineAlt;
    public GUIStyle Style_Selected;
    public GUIStyle Style_SelectedCell;
}
