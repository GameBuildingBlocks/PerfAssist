using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

public class PAEditorUtil
{
    public static object MemberValue(object obj, MemberInfo memInfo)
    {
        if (obj == null)
            return "";
        if (memInfo == null)
            return "";

        var pi = memInfo as PropertyInfo;
        if (pi != null)
        {
            return pi.GetValue(obj, null);
        }

        var fi = memInfo as FieldInfo;
        if (fi != null)
        {
            return fi.GetValue(obj);
        }

        return "";
    }

    public static string MemberToString(object obj, MemberInfo memInfo, string fmt)
    {
        object val = MemberValue(obj, memInfo);
        if (val == null)
            return "";

        if (fmt == PAEditorConst.BytesFormatter)
            return EditorUtility.FormatBytes((int)val);
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
        s_colorTextures.TryGetValue(c, out tex);
        if (tex == null) //Texture2D对象在游戏结束时为null
        {
            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, c);
            tex.Apply();

            s_colorTextures[c] = tex;
        }
        return tex;
    }

    public static void DrawLabel(string content, GUIStyle style)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(content, style, GUILayout.Width(style.CalcSize(new GUIContent(content)).x + 3));
        EditorGUILayout.EndHorizontal();
    }
}
