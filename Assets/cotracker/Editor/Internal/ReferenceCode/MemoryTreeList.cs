using System;
using UnityEngine;
using UnityEditor;

namespace CoInternal
{
    class MemoryTreeList
    {
        internal class Styles
        {
            public GUIStyle background = "OL Box";

            public GUIStyle header = "OL title";

            public GUIStyle entryEven = "OL EntryBackEven";

            public GUIStyle entryOdd = "OL EntryBackOdd";

            public GUIStyle numberLabel = "OL Label";

            public GUIStyle foldout = "IN foldout";
        }

        private const float kIndentPx = 16f;

        private const float kBaseIndent = 4f;

        protected const float kSmallMargin = 4f;

        protected const float kRowHeight = 16f;

        protected const float kNameColumnSize = 300f;

        protected const float kColumnSize = 70f;

        protected const float kFoldoutSize = 14f;

        private static MemoryTreeList.Styles m_Styles;

        public MemoryElementSelection m_MemorySelection;

        protected MemoryElement m_Root;

        protected EditorWindow m_EditorWindow;

        protected SplitterState m_Splitter;

        protected MemoryTreeList m_DetailView;

        protected Vector2 m_ScrollPosition;

        protected float m_SelectionOffset;

        protected float m_VisibleHeight;

        protected static MemoryTreeList.Styles styles
        {
            get
            {
                MemoryTreeList.Styles arg_17_0;
                if ((arg_17_0 = MemoryTreeList.m_Styles) == null)
                {
                    arg_17_0 = (MemoryTreeList.m_Styles = new MemoryTreeList.Styles());
                }
                return arg_17_0;
            }
        }

        public MemoryTreeList(EditorWindow editorWindow, MemoryTreeList detailview)
        {
            this.m_MemorySelection = new MemoryElementSelection();
            this.m_EditorWindow = editorWindow;
            this.m_DetailView = detailview;
            this.SetupSplitter();
        }

        protected virtual void SetupSplitter()
        {
            float[] array = new float[1];
            int[] array2 = new int[1];
            array[0] = 300f;
            array2[0] = 100;
            this.m_Splitter = new SplitterState(array, array2, null);
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical();
            //SplitterGUILayout.BeginHorizontalSplit(this.m_Splitter, EditorStyles.toolbar, new GUILayoutOption[0]);
            this.DrawHeader();
            //SplitterGUILayout.EndHorizontalSplit();
            if (this.m_Root == null)
            {
                GUILayout.EndVertical();
                return;
            }
            this.HandleKeyboard();
            this.m_ScrollPosition = GUILayout.BeginScrollView(this.m_ScrollPosition, MemoryTreeList.styles.background);
            int num = 0;
            foreach (MemoryElement current in this.m_Root.children)
            {
                this.DrawItem(current, ref num, 1);
                num++;
            }
            GUILayoutUtility.GetRect(0f, (float)num * 16f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                this.m_VisibleHeight = this.m_EditorWindow.position.height;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static float Clamp(float value, float min, float max)
        {
            return (value >= min) ? ((value <= max) ? value : max) : min;
        }

        public void SetRoot(MemoryElement root)
        {
            this.m_Root = root;
            if (this.m_Root != null)
            {
                this.m_Root.ExpandChildren();
            }
            if (this.m_DetailView != null)
            {
                this.m_DetailView.SetRoot(null);
            }
        }

        public MemoryElement GetRoot()
        {
            return this.m_Root;
        }

        protected void DrawBackground(int row, bool selected)
        {
            Rect position = GenerateRect(row);
            GUIStyle gUIStyle = (row % 2 != 0) ? MemoryTreeList.styles.entryOdd : MemoryTreeList.styles.entryEven;
            if (Event.current.type == EventType.Repaint)
            {
                gUIStyle.Draw(position, GUIContent.none, false, false, selected, false);
            }
        }

        protected virtual void DrawHeader()
        {
            GUILayout.Label("Referenced By:", MemoryTreeList.styles.header);
        }

        protected Rect GenerateRect(int row)
        {
            Rect result = new Rect(1f, 16f * (float)row, this.m_EditorWindow.position.width, 16f);
            return result;
        }

        protected virtual void DrawData(Rect rect, MemoryElement memoryElement, int indent, int row, bool selected)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }
            string text = memoryElement.name + "(" + memoryElement.memoryInfo.className + ")";
            MemoryTreeList.styles.numberLabel.Draw(rect, text, false, false, false, selected);
        }

        protected void DrawRecursiveData(MemoryElement element, ref int row, int indent)
        {
            if (element.ChildCount() == 0)
            {
                return;
            }
            element.ExpandChildren();
            foreach (MemoryElement current in element.children)
            {
                row++;
                this.DrawItem(current, ref row, indent);
            }
        }

        protected virtual void DrawItem(MemoryElement memoryElement, ref int row, int indent)
        {
            bool flag = this.m_MemorySelection.isSelected(memoryElement);
            DrawBackground(row, flag);
            Rect rect = GenerateRect(row);
            rect.x = 4f + (float)indent * 16f - 14f;
            Rect position = rect;
            position.width = 14f;
            if (memoryElement.ChildCount() > 0)
            {
                memoryElement.expanded = GUI.Toggle(position, memoryElement.expanded, GUIContent.none, MemoryTreeList.styles.foldout);
            }
            rect.x += 14f;
            if (flag)
            {
                this.m_SelectionOffset = (float)row * 16f;
            }
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                this.RowClicked(Event.current, memoryElement);
            }
            this.DrawData(rect, memoryElement, indent, row, flag);
            if (memoryElement.expanded)
            {
                this.DrawRecursiveData(memoryElement, ref row, indent + 1);
            }
        }

        protected void RowClicked(Event evt, MemoryElement memoryElement)
        {
            this.m_MemorySelection.SetSelection(memoryElement);
            if (evt.clickCount == 2 && memoryElement.memoryInfo != null && memoryElement.memoryInfo.instanceId != 0)
            {
                Selection.instanceIDs = new int[0];
                Selection.activeInstanceID = memoryElement.memoryInfo.instanceId;
            }
            evt.Use();
            if (memoryElement.memoryInfo != null)
            {
                EditorGUIUtility.PingObject(memoryElement.memoryInfo.instanceId);
            }
            if (this.m_DetailView != null)
            {
                this.m_DetailView.SetRoot((memoryElement.memoryInfo != null) ? new MemoryElement(memoryElement.memoryInfo, false) : null);
            }
            this.m_EditorWindow.Repaint();
        }

        protected void HandleKeyboard()
        {
            Event current = Event.current;
            if (this.m_MemorySelection.Selected == null)
            {
                return;
            }
            KeyCode keyCode = current.keyCode;
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                    this.m_MemorySelection.MoveUp();
                    goto IL_1D0;
                case KeyCode.DownArrow:
                    this.m_MemorySelection.MoveDown();
                    goto IL_1D0;
                case KeyCode.RightArrow:
                    if (this.m_MemorySelection.Selected.ChildCount() > 0)
                    {
                        this.m_MemorySelection.Selected.expanded = true;
                    }
                    goto IL_1D0;
                case KeyCode.LeftArrow:
                    if (this.m_MemorySelection.Selected.expanded)
                    {
                        this.m_MemorySelection.Selected.expanded = false;
                    }
                    else
                    {
                        this.m_MemorySelection.MoveParent();
                    }
                    goto IL_1D0;
                case KeyCode.Insert:
                //IL_73:
                    if (keyCode != KeyCode.Return)
                    {
                        return;
                    }
                    if (this.m_MemorySelection.Selected.memoryInfo != null)
                    {
                        Selection.instanceIDs = new int[0];
                        Selection.activeInstanceID = this.m_MemorySelection.Selected.memoryInfo.instanceId;
                    }
                    goto IL_1D0;
                case KeyCode.Home:
                    this.m_MemorySelection.MoveFirst();
                    goto IL_1D0;
                case KeyCode.End:
                    this.m_MemorySelection.MoveLast();
                    goto IL_1D0;
                //case KeyCode.PageUp:
                //    {
                //        int num = Mathf.RoundToInt(this.m_VisibleHeight / 16f);
                //        for (int i = 0; i < num; i++)
                //        {
                //            this.m_MemorySelection.MoveUp();
                //        }
                //        goto IL_1D0;
                //    }
                //case KeyCode.PageDown:
                //    {
                //        int num = Mathf.RoundToInt(this.m_VisibleHeight / 16f);
                //        for (int j = 0; j < num; j++)
                //        {
                //            this.m_MemorySelection.MoveDown();
                //        }
                //        goto IL_1D0;
                //    }
            }
            //goto IL_73;
            return;

        IL_1D0:
            this.RowClicked(current, this.m_MemorySelection.Selected);
            this.EnsureVisible();
            this.m_EditorWindow.Repaint();
        }

        private void RecursiveFindSelected(MemoryElement element, ref int row)
        {
            if (this.m_MemorySelection.isSelected(element))
            {
                this.m_SelectionOffset = (float)row * 16f;
            }
            row++;
            if (!element.expanded || element.ChildCount() == 0)
            {
                return;
            }
            element.ExpandChildren();
            foreach (MemoryElement current in element.children)
            {
                this.RecursiveFindSelected(current, ref row);
            }
        }

        protected void EnsureVisible()
        {
            int num = 0;
            this.RecursiveFindSelected(this.m_Root, ref num);
            this.m_ScrollPosition.y = MemoryTreeList.Clamp(this.m_ScrollPosition.y, this.m_SelectionOffset - this.m_VisibleHeight, this.m_SelectionOffset - 16f);
        }
    }
}
