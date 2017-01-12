using UnityEngine;
using System.IO;

public class ResourceTrackerConst
{
    public static string shaderPropertyNameJsonPath =
    Path.Combine(Path.Combine(Application.persistentDataPath, "TestTools"), "ShaderPropertyNameRecord.json");
    public static char shaderPropertyNameJsonToken = '$';

    public static string FormatBytes(int bytes)
    {
        if (bytes < 0)
            return "error bytes";
        
        if (bytes<1024)
        {
            return bytes + "b";
        }
        else if (bytes < 1024 * 1024)
        {
            return bytes / 1024 + "kb";
        }
        else {
            return bytes / 1024 /1024 + "mb";
        }
    }
}
