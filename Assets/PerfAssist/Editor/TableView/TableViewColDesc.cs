using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

//public enum FieldSizeType
//{
//    None,
//    ByPixels,
//    ByPercents,
//}

public class TableViewColDesc
{
    public string PropertyName;
    public string TitleText;

    public TextAnchor Alignment;
    public string Format;
    public float WidthInPercent;

    public FieldInfo fieldInfo;

    public string FormatObject(object obj)
    {
        return PAUtil.FieldToString(obj, fieldInfo, Format);
    }

    public int Compare(object o1, object o2)
    {
        object fv1 = PAUtil.FieldValue(o1, fieldInfo);
        object fv2 = PAUtil.FieldValue(o2, fieldInfo);

        IComparable fc1 = fv1 as IComparable;
        IComparable fc2 = fv2 as IComparable;
        if (fc1 == null || fc2 == null)
        {
            return fv1.ToString().CompareTo(fv2.ToString());
        }

        return fc1.CompareTo(fc2);
    }
}

