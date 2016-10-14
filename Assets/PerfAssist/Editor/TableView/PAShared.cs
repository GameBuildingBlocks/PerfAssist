using UnityEngine;
using System.Collections;
using System.Reflection;

public class PAConst
{
    public readonly static Color SelectionColor = (Color)new Color32(62, 95, 150, 255);
    public readonly static Color SelectionColorDark = (Color)new Color32(62, 95, 150, 128);
}

public class PAUtil
{
    public static object FieldValue(object obj, FieldInfo fieldInfo)
    {
        if (obj == null)
            return "";
        if (fieldInfo == null)
            return "";

        return fieldInfo.GetValue(obj);
    }
    public static string FieldToString(object obj, FieldInfo fieldInfo, string fmt)
    {
        object val = FieldValue(obj, fieldInfo);
        if (val == null)
            return "";
        if (val is float)
            return ((float)val).ToString(fmt);
        if (val is double)
            return ((double)val).ToString(fmt);
        return val.ToString();
    }
}
