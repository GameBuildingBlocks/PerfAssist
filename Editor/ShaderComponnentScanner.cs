using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using System.Text;
using PerfAssist.LitJson;
using System.IO;
using System;

public class ShaderComponnentScanner
{
#if UNITY_EDITOR
    [MenuItem(PAEditorConst.DevCommandPath + "/ShaderPropertyNameScanner")]
    static void ScanerShaderObj()
    {
        UnityEngine.Object[] allAssets = Resources.LoadAll("");

        Dictionary<string, string> scanerResultDict = new Dictionary<string, string>();
        foreach (UnityEngine.Object asset in allAssets)
        {
            Shader shader = asset as Shader;
            if (shader != null)
            {
                StringBuilder propertyNameSB = new StringBuilder();
                int propertyCount = ShaderUtil.GetPropertyCount(shader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        if (!string.IsNullOrEmpty(propertyName))
                        {
                            if (!string.IsNullOrEmpty(propertyNameSB.ToString()))
                                propertyNameSB.Append(ResourceTrackerConst.shaderPropertyNameJsonToken);
                            propertyNameSB.Append(propertyName);
                        }
                    }
                }
                if (!scanerResultDict.ContainsKey(shader.name))
                    scanerResultDict.Add(shader.name, propertyNameSB.ToString());
            }
        }

        Dictionary<string, string> shaderPropertyDict = new Dictionary<string, string>();
        foreach (var pair in scanerResultDict)
        {
            shaderPropertyDict.Add(pair.Key, pair.Value);
        }

        try
        {
            string memConfigPath = Path.Combine(Application.persistentDataPath, "TestTools");
            if (!Directory.Exists(memConfigPath))
                Directory.CreateDirectory(memConfigPath);

            string memResourceTrackerPath = Path.Combine(memConfigPath, "ResourceTracker");
            if (!Directory.Exists(memResourceTrackerPath))
                Directory.CreateDirectory(memResourceTrackerPath);

            StreamWriter sw;
            FileInfo fileInfo = new FileInfo(ResourceTrackerConst.shaderPropertyNameJsonPath);
            sw = fileInfo.CreateText();
            sw.Write(JsonUtility.ToJson(new Serialization<string, string>(shaderPropertyDict)));
            sw.Close();
            sw.Dispose();
            UnityEngine.Debug.Log("write shaderProperty Json successed");
            EditorUtility.DisplayDialog("write shaderProperty Json successed", "write shaderProperty Json successed", "确认");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogErrorFormat("write shaderProperty json file error,errMsg = {0}", ex.Message);
            EditorUtility.DisplayDialog("write shaderProperty Json failed", string.Format("write shaderProperty Json failed ,errMsg = {0}", ex.Message), "确认");
        }
    }
#endif
}