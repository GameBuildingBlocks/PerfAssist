using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LitJson;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ResourceTrackerConst
{
    public static string shaderPropertyNameJsonPath =
    Path.Combine(Path.Combine(Application.persistentDataPath, "TestTools"), "ShaderPropertyNameRecord.json");
    public static char shaderPropertyNameJsonToken = '$';
}
