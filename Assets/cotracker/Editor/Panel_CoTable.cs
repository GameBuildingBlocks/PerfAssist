using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class CoTableEntry
{
    public int SeqID = -1;
    public string Name = "Foo";
    public int ExecSelectedCount = 0;
    public float ExecSelectedTime = 0.0f;
    public int ExecAccumCount = 0;
    public float ExecAccumTime = 0.0f;
}

public delegate void CoroutineSelectionHandler(int coSeqID);

public class Panel_CoTable 
{
    GUIStyle NameTitle;
    GUIStyle NameLabel;
    GUIStyle NameLabelDark;
    GUIStyle Selected;

    public static Panel_CoTable Instance = new Panel_CoTable();

    private CoroutineSelectionHandler _onCoroutineSelected;
    public event CoroutineSelectionHandler OnCoroutineSelected
    {
        add
        {
            _onCoroutineSelected -= value;
            _onCoroutineSelected += value;
        }
        remove
        {
            _onCoroutineSelected -= value;
        }
    }

    public Panel_CoTable()
    {
        NameTitle = new GUIStyle(EditorStyles.whiteBoldLabel);
        NameTitle.alignment = TextAnchor.MiddleCenter;
        NameTitle.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.7f, 0.2f, 0.5f));
        NameTitle.normal.textColor = Color.white;

        NameLabel = new GUIStyle(EditorStyles.whiteLabel);
        NameLabel.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.1f));
        NameLabel.normal.textColor = Color.white;

        NameLabelDark = new GUIStyle(EditorStyles.whiteLabel);
        NameLabelDark.normal.background = GuiUtil.getColorTexture(new Color(0.5f, 0.5f, 0.5f, 0.2f));
        NameLabelDark.normal.textColor = Color.white;

        Selected = new GUIStyle(EditorStyles.whiteLabel);
        Selected.normal.background = GuiUtil.getColorTexture(new Color(0.3f, 0.3f, 0.8f, 0.6f));
        Selected.normal.textColor = Color.white;
    }

    private CoTableEntry m_selected = null;
    private List<CoTableEntry> m_items = new List<CoTableEntry>();

    static float _lineHeight = 25;

    static float[] _starts = new float[5] { 0.0f, 0.58f, 0.64f, 0.74f, 0.86f };
    static float[] _ratios = new float[5] { 0.58f, 0.06f, 0.1f, 0.12f, 0.14f };

    private Rect LabelRect(float width, int slot, int pos)
    {
        return new Rect(width * _starts[slot], pos * _lineHeight, width * _ratios[slot], _lineHeight);
    }

    Dictionary<string, int> TitleSlots = new Dictionary<string, int>()
    {
        { "Name", 0 },
        { "Cnt", 1 },
        { "Time", 2 },
        { "Cnt_Sum", 3 },
        { "Time_Sum", 4 },
    };

    int _sortSlot = 0;

    private void DrawTitle(float width)
    {
        foreach (var item in TitleSlots)
        {
            Rect r = LabelRect(width, item.Value, 0);
            GUI.Label(r, item.Key + (_sortSlot == item.Value ? " ▼" : ""), NameTitle);
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                _sortSlot = item.Value;
                EditorWindow w = EditorWindow.GetWindow<EditorWindow>("CoroutineTrackerWindow");
                if (w != null)
                {
                    PerformResort();
                    w.Repaint();
                }
            }
        }
    }

    private void DrawLine(int pos, CoTableEntry entry, float width)
    {
        Rect r = new Rect(0, pos * _lineHeight, width, _lineHeight);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
        {
            m_selected = entry;

            if (_onCoroutineSelected != null)
                _onCoroutineSelected(m_selected.SeqID);
        }

        GUIStyle style = new GUIStyle((pos % 2 != 0) ? NameLabel : NameLabelDark);
        if (m_selected == entry)
        {
            style = Selected;
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
        s.fixedHeight = _lineHeight * m_items.Count;
        s.stretchWidth = true;
        Rect r = EditorGUILayout.BeginVertical(s);

        // !!! this silly line (empty label) is required by Unity to ensure the scroll bar appear as expected.
        GuiUtil.DrawLabel("", NameLabel);

        DrawTitle(r.width);
        for (int i = 0; i < m_items.Count; i++)
        {
            DrawLine(i + 1, m_items[i], r.width);
        }
        EditorGUILayout.EndVertical();
    }

    public void RefreshEntries(List<CoTableEntry> entries)
    {
        if (entries == null)
            return;

        m_items.Clear();
        m_items.AddRange(entries);

        PerformResort();
    }

    private void PerformResort()
    {
        m_items.Sort((s1, s2) =>
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
