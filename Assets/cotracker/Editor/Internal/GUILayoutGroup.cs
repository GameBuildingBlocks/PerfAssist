using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoInternal
{
    class GUILayoutGroup : GUILayoutEntry
    {
        public List<GUILayoutEntry> entries = new List<GUILayoutEntry>();

        public bool isVertical = true;

        public bool resetCoords;

        public float spacing;

        public bool sameSize = true;

        public bool isWindow;

        public int windowID = -1;

        private int m_Cursor;

        protected int m_StretchableCountX = 100;

        protected int m_StretchableCountY = 100;

        protected bool m_UserSpecifiedWidth;

        protected bool m_UserSpecifiedHeight;

        protected float m_ChildMinWidth = 100f;

        protected float m_ChildMaxWidth = 100f;

        protected float m_ChildMinHeight = 100f;

        protected float m_ChildMaxHeight = 100f;

        private readonly RectOffset m_Margin = new RectOffset();

        public override RectOffset margin
        {
            get
            {
                return this.m_Margin;
            }
        }

        public GUILayoutGroup()
            : base(0f, 0f, 0f, 0f, GUIStyle.none)
        {
        }

        public GUILayoutGroup(GUIStyle _style, GUILayoutOption[] options)
            : base(0f, 0f, 0f, 0f, _style)
        {
            if (options != null)
            {
                this.ApplyOptions(options);
            }
            this.m_Margin.left = _style.margin.left;
            this.m_Margin.right = _style.margin.right;
            this.m_Margin.top = _style.margin.top;
            this.m_Margin.bottom = _style.margin.bottom;
        }

        public override void ApplyOptions(GUILayoutOption[] options)
        {
            if (options == null)
            {
                return;
            }
            base.ApplyOptions(options);
            for (int i = 0; i < options.Length; i++)
            {
                GUILayoutOption gUILayoutOption = options[i];
                switch (gUILayoutOption.type)
                {
                    case GUILayoutOption.Type.fixedWidth:
                    case GUILayoutOption.Type.minWidth:
                    case GUILayoutOption.Type.maxWidth:
                        this.m_UserSpecifiedHeight = true;
                        break;
                    case GUILayoutOption.Type.fixedHeight:
                    case GUILayoutOption.Type.minHeight:
                    case GUILayoutOption.Type.maxHeight:
                        this.m_UserSpecifiedWidth = true;
                        break;
                    case GUILayoutOption.Type.spacing:
                        this.spacing = (float)((int)gUILayoutOption.value);
                        break;
                }
            }
        }

        protected override void ApplyStyleSettings(GUIStyle style)
        {
            base.ApplyStyleSettings(style);
            RectOffset margin = style.margin;
            this.m_Margin.left = margin.left;
            this.m_Margin.right = margin.right;
            this.m_Margin.top = margin.top;
            this.m_Margin.bottom = margin.bottom;
        }

        public void ResetCursor()
        {
            this.m_Cursor = 0;
        }

        public Rect PeekNext()
        {
            if (this.m_Cursor < this.entries.Count)
            {
                GUILayoutEntry gUILayoutEntry = this.entries[this.m_Cursor];
                return gUILayoutEntry.rect;
            }
            throw new ArgumentException(string.Concat(new object[]
			{
				"Getting control ",
				this.m_Cursor,
				"'s position in a group with only ",
				this.entries.Count,
				" controls when doing ",
				Event.current.rawType,
				"\nAborting"
			}));
        }

        public GUILayoutEntry GetNext()
        {
            if (this.m_Cursor < this.entries.Count)
            {
                GUILayoutEntry result = this.entries[this.m_Cursor];
                this.m_Cursor++;
                return result;
            }
            throw new ArgumentException(string.Concat(new object[]
			{
				"Getting control ",
				this.m_Cursor,
				"'s position in a group with only ",
				this.entries.Count,
				" controls when doing ",
				Event.current.rawType,
				"\nAborting"
			}));
        }

        public Rect GetLast()
        {
            if (this.m_Cursor == 0)
            {
                Debug.LogError("You cannot call GetLast immediately after beginning a group.");
                return GUILayoutEntry.kDummyRect;
            }
            if (this.m_Cursor <= this.entries.Count)
            {
                GUILayoutEntry gUILayoutEntry = this.entries[this.m_Cursor - 1];
                return gUILayoutEntry.rect;
            }
            Debug.LogError(string.Concat(new object[]
			{
				"Getting control ",
				this.m_Cursor,
				"'s position in a group with only ",
				this.entries.Count,
				" controls when doing ",
				Event.current.type
			}));
            return GUILayoutEntry.kDummyRect;
        }

        public void Add(GUILayoutEntry e)
        {
            this.entries.Add(e);
        }

        public override void CalcWidth()
        {
            if (this.entries.Count == 0)
            {
                this.maxWidth = (this.minWidth = (float)base.style.padding.horizontal);
                return;
            }
            int num = 0;
            int num2 = 0;
            this.m_ChildMinWidth = 0f;
            this.m_ChildMaxWidth = 0f;
            this.m_StretchableCountX = 0;
            //bool flag = true;
            if (this.isVertical)
            {
                foreach (GUILayoutEntry current in this.entries)
                {
                    current.CalcWidth();
                    RectOffset margin = current.margin;
                    //if (current.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    if (!flag)
                    //    {
                    //        num = Mathf.Min(margin.left, num);
                    //        num2 = Mathf.Min(margin.right, num2);
                    //    }
                    //    else
                    //    {
                    //        num = margin.left;
                    //        num2 = margin.right;
                    //        flag = false;
                    //    }
                    //    this.m_ChildMinWidth = Mathf.Max(current.minWidth + (float)margin.horizontal, this.m_ChildMinWidth);
                    //    this.m_ChildMaxWidth = Mathf.Max(current.maxWidth + (float)margin.horizontal, this.m_ChildMaxWidth);
                    //}
                    this.m_StretchableCountX += current.stretchWidth;
                }
                this.m_ChildMinWidth -= (float)(num + num2);
                this.m_ChildMaxWidth -= (float)(num + num2);
            }
            else
            {
                int num3 = 0;
                foreach (GUILayoutEntry current2 in this.entries)
                {
                    current2.CalcWidth();
                    RectOffset margin2 = current2.margin;
                    //if (current2.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    int num4;
                    //    if (!flag)
                    //    {
                    //        num4 = ((num3 <= margin2.left) ? margin2.left : num3);
                    //    }
                    //    else
                    //    {
                    //        num4 = 0;
                    //        flag = false;
                    //    }
                    //    this.m_ChildMinWidth += current2.minWidth + this.spacing + (float)num4;
                    //    this.m_ChildMaxWidth += current2.maxWidth + this.spacing + (float)num4;
                    //    num3 = margin2.right;
                    //    this.m_StretchableCountX += current2.stretchWidth;
                    //}
                    //else
                    {
                        this.m_ChildMinWidth += current2.minWidth;
                        this.m_ChildMaxWidth += current2.maxWidth;
                        this.m_StretchableCountX += current2.stretchWidth;
                    }
                }
                this.m_ChildMinWidth -= this.spacing;
                this.m_ChildMaxWidth -= this.spacing;
                if (this.entries.Count != 0)
                {
                    num = this.entries[0].margin.left;
                    num2 = num3;
                }
                else
                {
                    num2 = (num = 0);
                }
            }
            float num5;
            float num6;
            if (base.style != GUIStyle.none || this.m_UserSpecifiedWidth)
            {
                num5 = (float)Mathf.Max(base.style.padding.left, num);
                num6 = (float)Mathf.Max(base.style.padding.right, num2);
            }
            else
            {
                this.m_Margin.left = num;
                this.m_Margin.right = num2;
                num6 = (num5 = 0f);
            }
            this.minWidth = Mathf.Max(this.minWidth, this.m_ChildMinWidth + num5 + num6);
            if (this.maxWidth == 0f)
            {
                this.stretchWidth += this.m_StretchableCountX + ((!base.style.stretchWidth) ? 0 : 1);
                this.maxWidth = this.m_ChildMaxWidth + num5 + num6;
            }
            else
            {
                this.stretchWidth = 0;
            }
            this.maxWidth = Mathf.Max(this.maxWidth, this.minWidth);
            if (base.style.fixedWidth != 0f)
            {
                this.maxWidth = (this.minWidth = base.style.fixedWidth);
                this.stretchWidth = 0;
            }
        }

        public override void SetHorizontal(float x, float width)
        {
            base.SetHorizontal(x, width);
            if (this.resetCoords)
            {
                x = 0f;
            }
            RectOffset padding = base.style.padding;
            if (this.isVertical)
            {
                if (base.style != GUIStyle.none)
                {
                    foreach (GUILayoutEntry current in this.entries)
                    {
                        float num = (float)Mathf.Max(current.margin.left, padding.left);
                        float x2 = x + num;
                        float num2 = width - (float)Mathf.Max(current.margin.right, padding.right) - num;
                        if (current.stretchWidth != 0)
                        {
                            current.SetHorizontal(x2, num2);
                        }
                        else
                        {
                            current.SetHorizontal(x2, Mathf.Clamp(num2, current.minWidth, current.maxWidth));
                        }
                    }
                }
                else
                {
                    float num3 = x - (float)this.margin.left;
                    float num4 = width + (float)this.margin.horizontal;
                    foreach (GUILayoutEntry current2 in this.entries)
                    {
                        if (current2.stretchWidth != 0)
                        {
                            current2.SetHorizontal(num3 + (float)current2.margin.left, num4 - (float)current2.margin.horizontal);
                        }
                        else
                        {
                            current2.SetHorizontal(num3 + (float)current2.margin.left, Mathf.Clamp(num4 - (float)current2.margin.horizontal, current2.minWidth, current2.maxWidth));
                        }
                    }
                }
            }
            else
            {
                if (base.style != GUIStyle.none)
                {
                    float num5 = (float)padding.left;
                    float num6 = (float)padding.right;
                    if (this.entries.Count != 0)
                    {
                        num5 = Mathf.Max(num5, (float)this.entries[0].margin.left);
                        num6 = Mathf.Max(num6, (float)this.entries[this.entries.Count - 1].margin.right);
                    }
                    x += num5;
                    width -= num6 + num5;
                }
                float num7 = width - this.spacing * (float)(this.entries.Count - 1);
                float t = 0f;
                if (this.m_ChildMinWidth != this.m_ChildMaxWidth)
                {
                    t = Mathf.Clamp((num7 - this.m_ChildMinWidth) / (this.m_ChildMaxWidth - this.m_ChildMinWidth), 0f, 1f);
                }
                float num8 = 0f;
                if (num7 > this.m_ChildMaxWidth && this.m_StretchableCountX > 0)
                {
                    num8 = (num7 - this.m_ChildMaxWidth) / (float)this.m_StretchableCountX;
                }
                int num9 = 0;
                bool flag = true;
                foreach (GUILayoutEntry current3 in this.entries)
                {
                    float num10 = Mathf.Lerp(current3.minWidth, current3.maxWidth, t);
                    num10 += num8 * (float)current3.stretchWidth;
                    //if (current3.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    int num11 = current3.margin.left;
                    //    if (flag)
                    //    {
                    //        num11 = 0;
                    //        flag = false;
                    //    }
                    //    int num12 = (num9 <= num11) ? num11 : num9;
                    //    x += (float)num12;
                    //    num9 = current3.margin.right;
                    //}
                    current3.SetHorizontal(Mathf.Round(x), Mathf.Round(num10));
                    x += num10 + this.spacing;
                }
            }
        }

        public override void CalcHeight()
        {
            if (this.entries.Count == 0)
            {
                this.maxHeight = (this.minHeight = (float)base.style.padding.vertical);
                return;
            }
            int num = 0;
            int num2 = 0;
            this.m_ChildMinHeight = 0f;
            this.m_ChildMaxHeight = 0f;
            this.m_StretchableCountY = 0;
            if (this.isVertical)
            {
                int num3 = 0;
                bool flag = true;
                foreach (GUILayoutEntry current in this.entries)
                {
                    current.CalcHeight();
                    RectOffset margin = current.margin;
                    //if (current.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    int num4;
                    //    if (!flag)
                    //    {
                    //        num4 = Mathf.Max(num3, margin.top);
                    //    }
                    //    else
                    //    {
                    //        num4 = 0;
                    //        flag = false;
                    //    }
                    //    this.m_ChildMinHeight += current.minHeight + this.spacing + (float)num4;
                    //    this.m_ChildMaxHeight += current.maxHeight + this.spacing + (float)num4;
                    //    num3 = margin.bottom;
                    //    this.m_StretchableCountY += current.stretchHeight;
                    //}
                    //else
                    //{
                    //    this.m_ChildMinHeight += current.minHeight;
                    //    this.m_ChildMaxHeight += current.maxHeight;
                    //    this.m_StretchableCountY += current.stretchHeight;
                    //}
                }
                this.m_ChildMinHeight -= this.spacing;
                this.m_ChildMaxHeight -= this.spacing;
                if (this.entries.Count != 0)
                {
                    num = this.entries[0].margin.top;
                    num2 = num3;
                }
                else
                {
                    num = (num2 = 0);
                }
            }
            else
            {
                bool flag2 = true;
                foreach (GUILayoutEntry current2 in this.entries)
                {
                    current2.CalcHeight();
                    RectOffset margin2 = current2.margin;
                    //if (current2.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    if (!flag2)
                    //    {
                    //        num = Mathf.Min(margin2.top, num);
                    //        num2 = Mathf.Min(margin2.bottom, num2);
                    //    }
                    //    else
                    //    {
                    //        num = margin2.top;
                    //        num2 = margin2.bottom;
                    //        flag2 = false;
                    //    }
                    //    this.m_ChildMinHeight = Mathf.Max(current2.minHeight, this.m_ChildMinHeight);
                    //    this.m_ChildMaxHeight = Mathf.Max(current2.maxHeight, this.m_ChildMaxHeight);
                    //}
                    this.m_StretchableCountY += current2.stretchHeight;
                }
            }
            float num5;
            float num6;
            if (base.style != GUIStyle.none || this.m_UserSpecifiedHeight)
            {
                num5 = (float)Mathf.Max(base.style.padding.top, num);
                num6 = (float)Mathf.Max(base.style.padding.bottom, num2);
            }
            else
            {
                this.m_Margin.top = num;
                this.m_Margin.bottom = num2;
                num6 = (num5 = 0f);
            }
            this.minHeight = Mathf.Max(this.minHeight, this.m_ChildMinHeight + num5 + num6);
            if (this.maxHeight == 0f)
            {
                this.stretchHeight += this.m_StretchableCountY + ((!base.style.stretchHeight) ? 0 : 1);
                this.maxHeight = this.m_ChildMaxHeight + num5 + num6;
            }
            else
            {
                this.stretchHeight = 0;
            }
            this.maxHeight = Mathf.Max(this.maxHeight, this.minHeight);
            if (base.style.fixedHeight != 0f)
            {
                this.maxHeight = (this.minHeight = base.style.fixedHeight);
                this.stretchHeight = 0;
            }
        }

        public override void SetVertical(float y, float height)
        {
            base.SetVertical(y, height);
            if (this.entries.Count == 0)
            {
                return;
            }
            RectOffset padding = base.style.padding;
            if (this.resetCoords)
            {
                y = 0f;
            }
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
                float num3 = height - this.spacing * (float)(this.entries.Count - 1);
                float t = 0f;
                if (this.m_ChildMinHeight != this.m_ChildMaxHeight)
                {
                    t = Mathf.Clamp((num3 - this.m_ChildMinHeight) / (this.m_ChildMaxHeight - this.m_ChildMinHeight), 0f, 1f);
                }
                float num4 = 0f;
                if (num3 > this.m_ChildMaxHeight && this.m_StretchableCountY > 0)
                {
                    num4 = (num3 - this.m_ChildMaxHeight) / (float)this.m_StretchableCountY;
                }
                int num5 = 0;
                bool flag = true;
                foreach (GUILayoutEntry current in this.entries)
                {
                    float num6 = Mathf.Lerp(current.minHeight, current.maxHeight, t);
                    num6 += num4 * (float)current.stretchHeight;
                    //if (current.style != GUILayoutUtility.spaceStyle)
                    //{
                    //    int num7 = current.margin.top;
                    //    if (flag)
                    //    {
                    //        num7 = 0;
                    //        flag = false;
                    //    }
                    //    int num8 = (num5 <= num7) ? num7 : num5;
                    //    y += (float)num8;
                    //    num5 = current.margin.bottom;
                    //}
                    current.SetVertical(Mathf.Round(y), Mathf.Round(num6));
                    y += num6 + this.spacing;
                }
            }
            else if (base.style != GUIStyle.none)
            {
                foreach (GUILayoutEntry current2 in this.entries)
                {
                    float num9 = (float)Mathf.Max(current2.margin.top, padding.top);
                    float y2 = y + num9;
                    float num10 = height - (float)Mathf.Max(current2.margin.bottom, padding.bottom) - num9;
                    if (current2.stretchHeight != 0)
                    {
                        current2.SetVertical(y2, num10);
                    }
                    else
                    {
                        current2.SetVertical(y2, Mathf.Clamp(num10, current2.minHeight, current2.maxHeight));
                    }
                }
            }
            else
            {
                float num11 = y - (float)this.margin.top;
                float num12 = height + (float)this.margin.vertical;
                foreach (GUILayoutEntry current3 in this.entries)
                {
                    if (current3.stretchHeight != 0)
                    {
                        current3.SetVertical(num11 + (float)current3.margin.top, num12 - (float)current3.margin.vertical);
                    }
                    else
                    {
                        current3.SetVertical(num11 + (float)current3.margin.top, Mathf.Clamp(num12 - (float)current3.margin.vertical, current3.minHeight, current3.maxHeight));
                    }
                }
            }
        }

        public override string ToString()
        {
            string text = string.Empty;
            string text2 = string.Empty;
            for (int i = 0; i < GUILayoutEntry.indent; i++)
            {
                text2 += " ";
            }
            string text3 = text;
            text = string.Concat(new object[]
			{
				text3,
				base.ToString(),
				" Margins: ",
				this.m_ChildMinHeight,
				" {\n"
			});
            GUILayoutEntry.indent += 4;
            foreach (GUILayoutEntry current in this.entries)
            {
                text = text + current.ToString() + "\n";
            }
            text = text + text2 + "}";
            GUILayoutEntry.indent -= 4;
            return text;
        }
    }
}
