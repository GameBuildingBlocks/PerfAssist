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
        Style_Title.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.7f, 0.2f, 0.5f));
        Style_Title.normal.textColor = Color.white;

        Style_Line = new GUIStyle(EditorStyles.whiteLabel);
        Style_Line.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.1f));
        Style_Line.normal.textColor = Color.white;

        Style_LineAlt = new GUIStyle(EditorStyles.whiteLabel);
        Style_LineAlt.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        Style_LineAlt.normal.textColor = Color.white;

        Style_Selected = new GUIStyle(EditorStyles.whiteLabel);
        Style_Selected.normal.background = GuiUtil.getColorTexture(PAConst.SelectionColor);
        Style_Selected.normal.textColor = Color.white;

        Style_SelectedCell = new GUIStyle(EditorStyles.whiteLabel);
        Style_SelectedCell.normal.background = GuiUtil.getColorTexture(PAConst.SelectionColorDark);
        Style_SelectedCell.normal.textColor = Color.yellow;
    }

    public GUIStyle Style_Title;
    public GUIStyle Style_Line;
    public GUIStyle Style_LineAlt;
    public GUIStyle Style_Selected;
    public GUIStyle Style_SelectedCell;
}
