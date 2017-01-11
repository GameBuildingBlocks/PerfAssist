using UnityEngine;
using System.Collections;
using UnityEditor.MemoryProfiler;
using System.IO;
using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using MemoryProfilerWindow;
using PerfAssist.LitJson;
using System.Text;
public class SnapshotIOperator
{
    private string _now;
    private string _basePath;
    Dictionary<string, MemType> _types = new Dictionary<string, MemType>();
    Dictionary<int, MemCategory> _categories = new Dictionary<int, MemCategory>();

    public bool isSaved(int snapshotCount, eProfilerMode profilerMode, string ip = null)
    {
        DirectoryInfo TheFolder = new DirectoryInfo(combineBasepath(profilerMode, ip));
        if (!TheFolder.Exists)
            return false;
        if (TheFolder.GetFiles().Length != snapshotCount)
            return false;
        return true;
    }

    public SnapshotIOperator()
    {
        refreshRecordTime();
    }

    public string combineBasepath(eProfilerMode profilerMode, string ip = null)
    {
        string mode = "";
        if (profilerMode == eProfilerMode.Remote)
        {
            if (ip == null)
                ip = "";
            mode = "-Remote-" + ip;
        }
        else if (profilerMode == eProfilerMode.Editor)
        {
            mode = "-Editor";
        }
        return Path.Combine(MemUtil.SnapshotsDir, _now + mode);
    }


    bool savePackedInfoByJson(string output, CrawledMemorySnapshot unpacked)
    {
        try
        {
            var resolveJson = resolvePackedForJson(unpacked);
            if (string.IsNullOrEmpty(resolveJson))
                throw new Exception("Resolve Json Data Failed");

            StreamWriter sw;
            FileInfo fileInfo = new FileInfo(output);
            sw = fileInfo.CreateText();
            sw.Write(resolveJson);
            sw.Close();
            sw.Dispose();
            UnityEngine.Debug.Log("write Json successed");
            return true;
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogErrorFormat("write json file error {0},errMsg = {1}", output, ex.Message);
            return false;
        }
    }

    private string resolvePackedForJson(CrawledMemorySnapshot packed)
    {
        if (packed == null)
            return null;
        var _unpacked = packed;
        _types.Clear();
        _categories.Clear();
        foreach (ThingInMemory thingInMemory in packed.allObjects)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;
            int category = MemUtil.GetCategory(thingInMemory);
            MemObject item = new MemObject(thingInMemory, _unpacked);
            MemType theType;
            if (!_types.ContainsKey(typeName))
            {
                theType = new MemType();
                theType.TypeName = MemUtil.GetCategoryLiteral(thingInMemory) + typeName;
                theType.Category = category;
                theType.Objects = new List<object>();
                _types.Add(typeName, theType);
            }
            else
            {
                theType = _types[typeName];
            }
            theType.AddObject(item);
        }

        //协议格式:
        //Data:
        //"obj" = "TypeName,Category,Count,size"
        //"info" ="RefCount,size,InstanceName,address,typeDescriptionIndex"
        //TypeDescs:
        //InstanceNames:

        Dictionary<int, string> typeDescDict = new Dictionary<int, string>();
        Dictionary<int, string> instanceNameDict = new Dictionary<int, string>();
        var jsonData = new JsonData();
        foreach (var type in _types)
        {
            var typeData = new JsonData();
            typeData["Obj"] = type.Key + "," + type.Value.Category + "," + type.Value.Count + "," + type.Value.Size;

            var objectDatas = new JsonData();
            foreach (var obj in type.Value.Objects)
            {
                var objectData = new JsonData();
                var memObj = obj as MemObject;
                string dataInfo;
                var instanceNameHash = memObj.InstanceName.GetHashCode();
                if (!instanceNameDict.ContainsKey(instanceNameHash))
                {
                    instanceNameDict.Add(instanceNameHash, memObj.InstanceName);
                }

                dataInfo = memObj.RefCount + "," + memObj.Size + "," + instanceNameDict[instanceNameHash];
                if (type.Value.Category == 2)
                {
                    var manged = memObj._thing as ManagedObject;
                    var typeDescriptionHash = manged.typeDescription.name.GetHashCode();
                    if (!typeDescDict.ContainsKey(typeDescriptionHash))
                    {
                        typeDescDict.Add(typeDescriptionHash, manged.typeDescription.name);
                    }
                    dataInfo += "," + Convert.ToString((int)manged.address, 16) + "," + typeDescriptionHash;
                }
                objectData["info"] = dataInfo;
                objectDatas.Add(objectData);
            }
            typeData["memObj"] = objectDatas;
            jsonData.Add(typeData);
        }
        var resultJson = new JsonData();
        resultJson["Data"] = jsonData;

        StringBuilder sb = new StringBuilder();
        foreach (var key in typeDescDict.Keys)
        {
            sb.Append("[[" + key + "]:" + typeDescDict[key] + "],");
        }
        resultJson["TypeDescs"] = sb.ToString();
        sb.Remove(0, sb.Length);

        foreach (var key in instanceNameDict.Keys)
        {
            sb.Append("[[" + key + "]:" + instanceNameDict[key] + "],");
        }
        resultJson["InstanceNames"] = sb.ToString();
        return resultJson.ToJson();
    }

    private bool _saveSnapshotJson(int fileName, CrawledMemorySnapshot unpacked)
    {
        try
        {
            string jsonPath = Path.Combine(_basePath, "json");
            var jsonDir = new DirectoryInfo(jsonPath);
            if (!jsonDir.Exists)
                jsonDir.Create();

            string jsonFile = Path.Combine(jsonPath, fileName + ".json");
            return savePackedInfoByJson(jsonFile, unpacked);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return false;
        }
    }

    public void refreshRecordTime()
    {
        _now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", new System.Globalization.DateTimeFormatInfo());
    }

    public bool saveSnapshotSessions(PackedMemorySnapshot snapshot, int sessionIndex, eProfilerMode profilerMode, string ip = null)
    {
        try
        {
            _basePath = combineBasepath(profilerMode, ip);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
            string fileName = Path.Combine(_basePath, string.Format("{0}.memsnap", sessionIndex));
            if (!File.Exists(fileName))
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                using (Stream stream = File.Open(fileName, FileMode.Create))
                {
                    bf.Serialize(stream, snapshot);
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format("save snapshot error ! msg ={0}", ex.Message));
            Debug.LogException(ex);
            return false;
        }
    }

    public bool saveSnapshotJsonFile(CrawledMemorySnapshot unpacked, int sessionIndex, eProfilerMode profilerMode, string ip = null)
    {
        try
        {
            _basePath = combineBasepath(profilerMode, ip);
            if (!Directory.Exists(_basePath))
                Directory.CreateDirectory(_basePath);
            _saveSnapshotJson(sessionIndex, unpacked);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format("save snapshot json error ! msg ={0}", ex.Message));
            Debug.LogException(ex);
            return false;
        }
    }
}