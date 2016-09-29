using System;
using UnityEngine;

namespace CoInternal
{
    public sealed class GUILayoutOption
    {
        public enum Type
        {
            fixedWidth,
            fixedHeight,
            minWidth,
            maxWidth,
            minHeight,
            maxHeight,
            stretchWidth,
            stretchHeight,
            alignStart,
            alignMiddle,
            alignEnd,
            alignJustify,
            equalSize,
            spacing
        }

        public GUILayoutOption.Type type;

        public object value;

        public GUILayoutOption(GUILayoutOption.Type type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }
    class GUILayoutEntry
    {
        public float minWidth;

        public float maxWidth;

        public float minHeight;

        public float maxHeight;

        public Rect rect = new Rect(0f, 0f, 0f, 0f);

        public int stretchWidth;

        public int stretchHeight;

        private GUIStyle m_Style = GUIStyle.none;

        internal static Rect kDummyRect = new Rect(0f, 0f, 1f, 1f);

        protected static int indent = 0;

        public GUIStyle style
        {
            get
            {
                return this.m_Style;
            }
            set
            {
                this.m_Style = value;
                this.ApplyStyleSettings(value);
            }
        }

        public virtual RectOffset margin
        {
            get
            {
                return this.style.margin;
            }
        }

        public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style)
        {
            this.minWidth = _minWidth;
            this.maxWidth = _maxWidth;
            this.minHeight = _minHeight;
            this.maxHeight = _maxHeight;
            if (_style == null)
            {
                _style = GUIStyle.none;
            }
            this.style = _style;
        }

        public GUILayoutEntry(float _minWidth, float _maxWidth, float _minHeight, float _maxHeight, GUIStyle _style, GUILayoutOption[] options)
        {
            this.minWidth = _minWidth;
            this.maxWidth = _maxWidth;
            this.minHeight = _minHeight;
            this.maxHeight = _maxHeight;
            this.style = _style;
            this.ApplyOptions(options);
        }

        public virtual void CalcWidth()
        {
        }

        public virtual void CalcHeight()
        {
        }

        public virtual void SetHorizontal(float x, float width)
        {
            this.rect.x = x;
            this.rect.width = width;
        }

        public virtual void SetVertical(float y, float height)
        {
            this.rect.y = y;
            this.rect.height = height;
        }

        protected virtual void ApplyStyleSettings(GUIStyle style)
        {
            this.stretchWidth = ((style.fixedWidth != 0f || !style.stretchWidth) ? 0 : 1);
            this.stretchHeight = ((style.fixedHeight != 0f || !style.stretchHeight) ? 0 : 1);
            this.m_Style = style;
        }

        public virtual void ApplyOptions(GUILayoutOption[] options)
        {
            if (options == null)
            {
                return;
            }
            for (int i = 0; i < options.Length; i++)
            {
                GUILayoutOption gUILayoutOption = options[i];
                switch (gUILayoutOption.type)
                {
                    case GUILayoutOption.Type.fixedWidth:
                        this.minWidth = (this.maxWidth = (float)gUILayoutOption.value);
                        this.stretchWidth = 0;
                        break;
                    case GUILayoutOption.Type.fixedHeight:
                        this.minHeight = (this.maxHeight = (float)gUILayoutOption.value);
                        this.stretchHeight = 0;
                        break;
                    case GUILayoutOption.Type.minWidth:
                        this.minWidth = (float)gUILayoutOption.value;
                        if (this.maxWidth < this.minWidth)
                        {
                            this.maxWidth = this.minWidth;
                        }
                        break;
                    case GUILayoutOption.Type.maxWidth:
                        this.maxWidth = (float)gUILayoutOption.value;
                        if (this.minWidth > this.maxWidth)
                        {
                            this.minWidth = this.maxWidth;
                        }
                        this.stretchWidth = 0;
                        break;
                    case GUILayoutOption.Type.minHeight:
                        this.minHeight = (float)gUILayoutOption.value;
                        if (this.maxHeight < this.minHeight)
                        {
                            this.maxHeight = this.minHeight;
                        }
                        break;
                    case GUILayoutOption.Type.maxHeight:
                        this.maxHeight = (float)gUILayoutOption.value;
                        if (this.minHeight > this.maxHeight)
                        {
                            this.minHeight = this.maxHeight;
                        }
                        this.stretchHeight = 0;
                        break;
                    case GUILayoutOption.Type.stretchWidth:
                        this.stretchWidth = (int)gUILayoutOption.value;
                        break;
                    case GUILayoutOption.Type.stretchHeight:
                        this.stretchHeight = (int)gUILayoutOption.value;
                        break;
                }
            }
            if (this.maxWidth != 0f && this.maxWidth < this.minWidth)
            {
                this.maxWidth = this.minWidth;
            }
            if (this.maxHeight != 0f && this.maxHeight < this.minHeight)
            {
                this.maxHeight = this.minHeight;
            }
        }

        public override string ToString()
        {
            string text = string.Empty;
            for (int i = 0; i < GUILayoutEntry.indent; i++)
            {
                text += " ";
            }
            return string.Concat(new object[]
			{
				text,
				string.Format("{1}-{0} (x:{2}-{3}, y:{4}-{5})", (this.style == null) ? "NULL" : this.style.name,
					base.GetType(),
					this.rect.x,
					this.rect.xMax,
					this.rect.y,
					this.rect.yMax
				),
				"   -   W: ",
				this.minWidth,
				"-",
				this.maxWidth,
				(this.stretchWidth == 0) ? string.Empty : "+",
				", H: ",
				this.minHeight,
				"-",
				this.maxHeight,
				(this.stretchHeight == 0) ? string.Empty : "+"
			});
        }
    }
}
