using System.IO;
using UnityEngine;

public class ResourceTrackerConst
{
    public static string shaderPropertyNameJsonPath =
    Path.Combine(Path.Combine(Application.persistentDataPath, "TestTools"), "ShaderPropertyNameRecord.json");
    public static char shaderPropertyNameJsonToken = '$';
}
