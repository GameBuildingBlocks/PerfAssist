using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class GuiConstants
{
    public static float ToobarHeight = 30.0f;
    public static float DataTableWidth = 400.0f;
}

public static class GuiUtil
{
    public static void DrawLabel(string content, GUIStyle style)
    {
        EditorGUILayout.LabelField(content, style, GUILayout.Width(style.CalcSize(new GUIContent(content)).x + 5));
    }

    private static Dictionary<Color, Texture2D> s_colorTextures = new Dictionary<Color,Texture2D>();
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
