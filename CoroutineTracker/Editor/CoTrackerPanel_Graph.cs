using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public delegate void CoGraphSelectionHandler(int selectionIndex);

public class CoTrackerPanel_Graph 
{
    static Vector2 mScrollPos;
    static float mWidth;

    static int mMouseOverGraphIndex = -1;
    static float mMouseX = 0;
    static float mSelectedXLeft = -1.0f;
    static float mSelectedX = -1.0f;

    static float x_offset = 2.0f;
    static float y_gap = 5.0f;
    static float y_offset = 2;

    static GUIStyle NameLabel;
    static GUIStyle SmallLabel;
    static GUIStyle HoverText;

    static Material mLineMaterial;

    static Color LabelBackground = new Color(0.1f, 0.1f, 0.1f, 0.9f);

    private CoGraphSelectionHandler _selectionChanged;
    public event CoGraphSelectionHandler SelectionChanged
    {
        add
        {
            _selectionChanged -= value;
            _selectionChanged += value;
        }
        remove
        {
            _selectionChanged -= value;
        }
    }

    public static CoTrackerPanel_Graph Instance = new CoTrackerPanel_Graph();

    static void InitializeStyles()
    {
        if (NameLabel == null)
        {
            NameLabel = new GUIStyle(EditorStyles.whiteBoldLabel);
            NameLabel.normal.background = PAEditorUtil.getColorTexture(LabelBackground);
            NameLabel.normal.textColor = Color.white;

            SmallLabel = new GUIStyle(EditorStyles.whiteLabel);
            SmallLabel.normal.background = PAEditorUtil.getColorTexture(LabelBackground);
            SmallLabel.normal.textColor = Color.white;

            HoverText = new GUIStyle(EditorStyles.whiteLabel);
            HoverText.alignment = TextAnchor.MiddleCenter;
            HoverText.normal.background = PAEditorUtil.getColorTexture(LabelBackground);
            HoverText.normal.textColor = Color.white;
        }
    }

    static void DrawGraphGrid(float y_pos, float width, float height, float steps, Color c)
    {
        GL.Color(c);
        float x_step = width / steps;
        float y_step = height / steps;
        for (int i = 0; i < steps + 1; ++i)
        {
            Plot(x_offset + x_step * i, y_pos, x_offset + x_step * i, y_pos + height);
            Plot(x_offset, y_pos + y_step * i, x_offset + width, y_pos + y_step * i);
        }
    }

    static void DrawGraphGridLines(float y_pos, float width, float height, bool draw_mouse_line)
    {
        if (height >= 200)
        {
            DrawGraphGrid(y_pos, width, height, 8, new Color(0.25f, 0.25f, 0.25f));
        }
        if (height >= 100)
        {
            DrawGraphGrid(y_pos, width, height, 4, new Color(0.25f, 0.25f, 0.25f));
        }
        {
            DrawGraphGrid(y_pos, width, height, 2, new Color(0.3f, 0.3f, 0.3f));
            DrawGraphGrid(y_pos, width, height, 1, new Color(0.4f, 0.4f, 0.4f));
        }

        if (draw_mouse_line)
        {
            GL.Color(new Color(0.8f, 0.8f, 0.8f));
            Plot(mMouseX, y_pos, mMouseX, y_pos + height);
        }

        if (mSelectedXLeft >= 0)
        {
            GL.Color(new Color(0.5f, 0.9f, 0.6f));
            Plot(mSelectedXLeft, y_pos, mSelectedXLeft, y_pos + height);
            Plot(mSelectedX, y_pos, mSelectedX, y_pos + height);
        }
    }

    static void Plot(float x0, float y0, float x1, float y1)
    {
        GL.Vertex3(x0, y0, 0);
        GL.Vertex3(x1, y1, 0);
    }

    static void CreateLineMaterial()
    {
        if (!mLineMaterial)
        {
            mLineMaterial = new Material(Shader.Find("Custom/GraphIt"));
            mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
            mLineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    public void DrawGraphs(Rect rect)
    {
        if (GraphIt.Instance != null)
        {
            InitializeStyles();
            CreateLineMaterial();

            mLineMaterial.SetPass(0);

            int graph_index = 0;

            //use this to get the starting y position for the GL rendering
            Rect find_y = EditorGUILayout.BeginVertical(GUIStyle.none);
            EditorGUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
            {
                GL.PushMatrix();
                float start_y = find_y.y;
                GL.Viewport(new Rect(0, 0, rect.width, rect.height - start_y));
                GL.LoadPixelMatrix(0, rect.width, rect.height - start_y, 0);


                //Draw grey BG
                GL.Begin(GL.QUADS);
                GL.Color(new Color(0.2f, 0.2f, 0.2f));

                float scrolled_y_pos = y_offset - mScrollPos.y;
                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    if (kv.Value.GetHidden())
                    {
                        continue;
                    }

                    float height = kv.Value.GetHeight();

                    GL.Vertex3(x_offset, scrolled_y_pos, 0);
                    GL.Vertex3(x_offset + mWidth, scrolled_y_pos, 0);
                    GL.Vertex3(x_offset + mWidth, scrolled_y_pos + height, 0);
                    GL.Vertex3(x_offset, scrolled_y_pos + height, 0);

                    scrolled_y_pos += (height + y_gap);
                }
                GL.End();

                //Draw Lines
                GL.Begin(GL.LINES);
                scrolled_y_pos = y_offset - mScrollPos.y;

                foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
                {
                    if ( kv.Value.GetHidden() )
                    {
                        continue;
                    }
                    graph_index++;

                    float x_step = mWidth / kv.Value.GraphFullLength();

                    float height = kv.Value.GetHeight();
                    DrawGraphGridLines(scrolled_y_pos, mWidth, height, mMouseOverGraphIndex != -1);

                    if (kv.Value.GraphLength() > 0)
                    {
                        foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                        {
                            GraphItDataInternal g = entry.Value;

                            float y_min = kv.Value.GetMin(entry.Key);
                            float y_max = kv.Value.GetMax(entry.Key);
                            float y_range = Mathf.Max(y_max - y_min, 0.00001f);

                            //draw the 0 line
                            if (y_max > 0.0f && y_min < 0.0f)
                            {
                                GL.Color(g.mColor * 0.5f);
                                float y = scrolled_y_pos + height * (1 - (0.0f - y_min) / y_range);
                                Plot(x_offset, y, x_offset + mWidth, y);
                            }


                            GL.Color(g.mColor);

                            float previous_value = 0;
                            int start_index = (kv.Value.mCurrentIndex) % kv.Value.GraphLength();
                            for (int i = 0; i < kv.Value.GraphLength(); ++i)
                            {
                                float value = g.mDataPoints[start_index];
                                if (i >= 1)
                                {
                                    float x0 = x_offset + (i - 1) * x_step;
                                    float y0 = scrolled_y_pos + height * (1 - (previous_value - y_min) / y_range);

                                    float x1 = x_offset + i * x_step;
                                    float y1 = scrolled_y_pos + height * (1 - (value - y_min) / y_range);

                                    Plot(x0, y0, x1, y1);
                                }
                                previous_value = value;
                                start_index = (start_index + 1) % kv.Value.GraphFullLength();
                            }
                        }
                    }

                    scrolled_y_pos += (height + y_gap);
                }                
                GL.End();

                GL.PopMatrix();
            
                GL.Viewport(new Rect(0, 0, rect.width, rect.height));
                GL.LoadPixelMatrix(0, rect.width, rect.height, 0);
            }

            mScrollPos = EditorGUILayout.BeginScrollView(mScrollPos, GUIStyle.none);
            graph_index = 0;
            if (Event.current.type == EventType.Repaint)
            {
                mMouseOverGraphIndex = -1; //clear it out every repaint to ensure when the mouse leaves we don't leave the pointer around
            }
            foreach (KeyValuePair<string, GraphItData> kv in GraphIt.Instance.Graphs)
            {
                if (kv.Value.GetHidden())
                {
                    continue;
                }
                graph_index++;
                
                float height = kv.Value.GetHeight();

                GUIStyle s = new GUIStyle();                
                s.fixedHeight = height + y_gap;
                s.stretchWidth = true;
                Rect r = EditorGUILayout.BeginVertical(s);
                if (r.width != -0)
                {
                    mWidth = r.width - 2 * x_offset;
                }

                string fmt = "###,###,###,##0.###";
                string fu_str = " " + (kv.Value.mFixedUpdate ? "(FixedUpdate)" : "");

                NameLabel.normal.textColor = Color.white;
                PAEditorUtil.DrawLabel(kv.Key + fu_str, NameLabel);

                foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                {
                    GraphItDataInternal g = entry.Value;
                    if (kv.Value.mData.Count > 1 || entry.Key != GraphIt.BASE_GRAPH)
                    {
                        NameLabel.normal.textColor = g.mColor;
                        PAEditorUtil.DrawLabel(entry.Key, NameLabel);
                    }

                    if (g.mDataPoints.Length > 0)
                    {
                        int index = kv.Value.mCurrentIndex == 0 ? g.mDataPoints.Length - 1 : (kv.Value.mCurrentIndex - 1) % g.mDataPoints.Length;
                        try
                        {
                            PAEditorUtil.DrawLabel(g.mDataPoints[index].ToString(fmt), SmallLabel);
                        }
                        catch (System.Exception)
                        {
                            Debug.LogWarningFormat("[CoGraph] invalid index (mCurrentIndex: {0}, modulized {1})", kv.Value.mCurrentIndex, index);
                        }
                    }
                }
                
                if (Event.current.type == EventType.MouseDrag && r.Contains(Event.current.mousePosition - Event.current.delta))
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
                    {
                        kv.Value.DoHeightDelta(Event.current.delta.y);
                    }
                }
                
                if (Event.current.type != EventType.Layout && r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                    {
                        mMouseOverGraphIndex = graph_index;
                        mMouseX = Event.current.mousePosition.x;
                        
                        float x_step = mWidth / kv.Value.GraphFullLength();
                        float hover_y_offset = 0;
                        if (kv.Value.GraphLength() > 0)
                        {
                            foreach (KeyValuePair<string, GraphItDataInternal> entry in kv.Value.mData)
                            {
                                GraphItDataInternal g = entry.Value;
                                
                                //walk through the data points to find the correct index matching the mouse position value
                                //potential optimization here to find the index algebraically.
                                int start_index = (kv.Value.mCurrentIndex) % kv.Value.GraphLength();
                                for (int i = 0; i < kv.Value.GraphLength(); ++i)
                                {
                                    float value = g.mDataPoints[start_index];
                                    if (i >= 1)
                                    {
                                        float x0 = x_offset + (i - 1) * x_step;
                                        float x1 = x_offset + i * x_step;
                                        if (x0 < mMouseX && mMouseX <= x1)
                                        {
                                            //found this mouse positions step

                                            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
                                            {
                                                mSelectedXLeft = x0;
                                                mSelectedX = x1;

                                                if (_selectionChanged != null)
                                                    _selectionChanged(start_index);
                                            }

                                            string text = value.ToString(fmt);

                                            Rect tooltip_r = new Rect(Event.current.mousePosition + new Vector2(10, 2 - hover_y_offset), new Vector2(50, 20));
                                            HoverText.normal.textColor = g.mColor;
                                            GUI.Label(tooltip_r, text, HoverText);

                                            hover_y_offset += 13;
                                            break;
                                        }
                                    }
                                    start_index = (start_index + 1) % kv.Value.GraphFullLength();
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}