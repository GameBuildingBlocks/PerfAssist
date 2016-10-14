using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;

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

    public static string GetRandomString()
    {
        string path = Path.GetRandomFileName();
        path = path.Replace(".", ""); // Remove period.
        return path;
    }

    private static Dictionary<Color, Texture2D> s_colorTextures = new Dictionary<Color, Texture2D>();
    public static Texture2D getColorTexture(Color c)
    {
        Texture2D tex = null;
        if (s_colorTextures.TryGetValue(c, out tex))
            return tex;

        tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, c);
        tex.Apply();

        s_colorTextures[c] = tex;
        return tex;
    }
}
