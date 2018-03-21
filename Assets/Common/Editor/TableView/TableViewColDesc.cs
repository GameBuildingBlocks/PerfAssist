using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

public class TableViewColDesc
{
    public string PropertyName;
    public string TitleText;

    public TextAnchor Alignment;
    public string Format;
    public float WidthInPercent;

    public MemberInfo MemInfo;

    public string FormatObject(object obj)
    {
        return PAEditorUtil.MemberToString(obj, MemInfo, Format);
    }

    public int Compare(object o1, object o2)
    {
        object fv1 = PAEditorUtil.MemberValue(o1, MemInfo);
        object fv2 = PAEditorUtil.MemberValue(o2, MemInfo);

        IComparable fc1 = fv1 as IComparable;
        IComparable fc2 = fv2 as IComparable;
        if (fc1 == null || fc2 == null)
        {
            return fv1.ToString().CompareTo(fv2.ToString());
        }

        return fc1.CompareTo(fc2);
    }
}
