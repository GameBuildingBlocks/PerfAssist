using System;
using UnityEditor;
using UnityEngine;

namespace CoInternal
{
    class SplitterGUILayout
    {
        internal class GUISplitterGroup : GUILayoutGroup
        {
            public SplitterState state;

            public override void SetHorizontal(float x, float width)
            {
                if (!this.isVertical)
                {
                    this.state.xOffset = x;
                    int i;
                    if (width != (float)this.state.lastTotalSize)
                    {
                        this.state.RelativeToRealSizes((int)width);
                        this.state.lastTotalSize = (int)width;
                        for (i = 0; i < this.state.realSizes.Length - 1; i++)
                        {
                            this.state.DoSplitter(i, i + 1, 0);
                        }
                    }
                    i = 0;
                    foreach (GUILayoutEntry current in this.entries)
                    {
                        float num = (float)this.state.realSizes[i];
                        current.SetHorizontal(Mathf.Round(x), Mathf.Round(num));
                        x += num + this.spacing;
                        i++;
                    }
                }
                else
                {
                    base.SetHorizontal(x, width);
                }
            }

            public override void SetVertical(float y, float height)
            {
                this.rect.y = y;
                this.rect.height = height;
                RectOffset padding = base.style.padding;
                if (this.isVertical)
                {
                    if (base.style != GUIStyle.none)
                    {
                        float num = (float)padding.top;
                        float num2 = (float)padding.bottom;
                        if (this.entries.Count != 0)
                        {
                            num = Mathf.Max(num, (float)this.entries[0].margin.top);
                            num2 = Mathf.Max(num2, (float)this.entries[this.entries.Count - 1].margin.bottom);
                        }
                        y += num;
                        height -= num2 + num;
                    }
                    int i;
                    if (height != (float)this.state.lastTotalSize)
                    {
                        this.state.RelativeToRealSizes((int)height);
                        this.state.lastTotalSize = (int)height;
                        for (i = 0; i < this.state.realSizes.Length - 1; i++)
                        {
                            this.state.DoSplitter(i, i + 1, 0);
                        }
                    }
                    i = 0;
                    foreach (GUILayoutEntry current in this.entries)
                    {
                        float num3 = (float)this.state.realSizes[i];
                        current.SetVertical(Mathf.Round(y), Mathf.Round(num3));
                        y += num3 + this.spacing;
                        i++;
                    }
                }
                else if (base.style != GUIStyle.none)
                {
                    foreach (GUILayoutEntry current2 in this.entries)
                    {
                        float num4 = (float)Mathf.Max(current2.margin.top, padding.top);
                        float y2 = y + num4;
                        float num5 = height - (float)Mathf.Max(current2.margin.bottom, padding.bottom) - num4;
                        if (current2.stretchHeight != 0)
                        {
                            current2.SetVertical(y2, num5);
                        }
                        else
                        {
                            current2.SetVertical(y2, Mathf.Clamp(num5, current2.minHeight, current2.maxHeight));
                        }
                    }
                }
                else
                {
                    float num6 = y - (float)this.margin.top;
                    float num7 = height + (float)this.margin.vertical;
                    foreach (GUILayoutEntry current3 in this.entries)
                    {
                        if (current3.stretchHeight != 0)
                        {
                            current3.SetVertical(num6 + (float)current3.margin.top, num7 - (float)current3.margin.vertical);
                        }
                        else
                        {
                            current3.SetVertical(num6 + (float)current3.margin.top, Mathf.Clamp(num7 - (float)current3.margin.vertical, current3.minHeight, current3.maxHeight));
                        }
                    }
                }
            }
        }

        private static int splitterHash = "Splitter".GetHashCode();

        public static void BeginSplit(SplitterState state, GUIStyle style, bool vertical, params GUILayoutOption[] options)
        {
            //SplitterGUILayout.GUISplitterGroup gUISplitterGroup = (SplitterGUILayout.GUISplitterGroup)GUILayoutUtility.BeginLayoutGroup(style, null, typeof(SplitterGUILayout.GUISplitterGroup));
            //state.ID = GUIUtility.GetControlID(SplitterGUILayout.splitterHash, FocusType.Native);
            //switch (Event.current.GetTypeForControl(state.ID))
            //{
            //    case EventType.MouseDown:
            //        if (Event.current.button == 0 && Event.current.clickCount == 1)
            //        {
            //            int num = (!gUISplitterGroup.isVertical) ? ((int)gUISplitterGroup.rect.x) : ((int)gUISplitterGroup.rect.y);
            //            int num2 = (!gUISplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
            //            for (int i = 0; i < state.relativeSizes.Length - 1; i++)
            //            {
            //                if (((!gUISplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num + (float)state.realSizes[i] - (float)(state.splitSize / 2), gUISplitterGroup.rect.y, (float)state.splitSize, gUISplitterGroup.rect.height) : new Rect(state.xOffset + gUISplitterGroup.rect.x, (float)(num + state.realSizes[i] - state.splitSize / 2), gUISplitterGroup.rect.width, (float)state.splitSize)).Contains(Event.current.mousePosition))
            //                {
            //                    state.splitterInitialOffset = num2;
            //                    state.currentActiveSplitter = i;
            //                    GUIUtility.hotControl = state.ID;
            //                    Event.current.Use();
            //                    break;
            //                }
            //                num += state.realSizes[i];
            //            }
            //        }
            //        break;
            //    case EventType.MouseUp:
            //        if (GUIUtility.hotControl == state.ID)
            //        {
            //            GUIUtility.hotControl = 0;
            //            state.currentActiveSplitter = -1;
            //            state.RealToRelativeSizes();
            //            Event.current.Use();
            //        }
            //        break;
            //    case EventType.MouseDrag:
            //        if (GUIUtility.hotControl == state.ID && state.currentActiveSplitter >= 0)
            //        {
            //            int num2 = (!gUISplitterGroup.isVertical) ? ((int)Event.current.mousePosition.x) : ((int)Event.current.mousePosition.y);
            //            int num3 = num2 - state.splitterInitialOffset;
            //            if (num3 != 0)
            //            {
            //                state.splitterInitialOffset = num2;
            //                state.DoSplitter(state.currentActiveSplitter, state.currentActiveSplitter + 1, num3);
            //            }
            //            Event.current.Use();
            //        }
            //        break;
            //    case EventType.Repaint:
            //        {
            //            int num4 = (!gUISplitterGroup.isVertical) ? ((int)gUISplitterGroup.rect.x) : ((int)gUISplitterGroup.rect.y);
            //            for (int j = 0; j < state.relativeSizes.Length - 1; j++)
            //            {
            //                Rect position = (!gUISplitterGroup.isVertical) ? new Rect(state.xOffset + (float)num4 + (float)state.realSizes[j] - (float)(state.splitSize / 2), gUISplitterGroup.rect.y, (float)state.splitSize, gUISplitterGroup.rect.height) : new Rect(state.xOffset + gUISplitterGroup.rect.x, (float)(num4 + state.realSizes[j] - state.splitSize / 2), gUISplitterGroup.rect.width, (float)state.splitSize);
            //                EditorGUIUtility.AddCursorRect(position, (!gUISplitterGroup.isVertical) ? MouseCursor.SplitResizeLeftRight : MouseCursor.ResizeVertical, state.ID);
            //                num4 += state.realSizes[j];
            //            }
            //            break;
            //        }
            //    case EventType.Layout:
            //        gUISplitterGroup.state = state;
            //        gUISplitterGroup.resetCoords = false;
            //        gUISplitterGroup.isVertical = vertical;
            //        gUISplitterGroup.ApplyOptions(options);
            //        break;
            //}
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            SplitterGUILayout.BeginSplit(state, GUIStyle.none, false, options);
        }

        public static void BeginVerticalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            SplitterGUILayout.BeginSplit(state, GUIStyle.none, true, options);
        }

        public static void BeginHorizontalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
        {
            SplitterGUILayout.BeginSplit(state, style, false, options);
        }

        public static void BeginVerticalSplit(SplitterState state, GUIStyle style, params GUILayoutOption[] options)
        {
            SplitterGUILayout.BeginSplit(state, style, true, options);
        }

        public static void EndVerticalSplit()
        {
            //GUILayoutUtility.EndLayoutGroup();
        }

        public static void EndHorizontalSplit()
        {
            //GUILayoutUtility.EndLayoutGroup();
        }
    }
}
