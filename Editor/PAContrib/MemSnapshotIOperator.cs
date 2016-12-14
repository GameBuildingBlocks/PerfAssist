using UnityEngine;
using System.Collections;
using UnityEditor.MemoryProfiler;
using System.IO;
using System;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
public class SnapshotIOperator {
    private int saveSnapshotIndex=0;
    private int savePathIndex=0;
    private bool isFristSave=true;
    private string _now;
    private string _basePath;

    public bool isSaved(int snapshotCount,eProfilerMode profilerMode, string ip = null)
    {
        DirectoryInfo TheFolder = new DirectoryInfo(combineBasepath(profilerMode, ip));
        if (!TheFolder.Exists || isFristSave)
            return false;
        if (TheFolder.GetFiles().Length!= snapshotCount)
            return false;
        return  true;
    }

    public SnapshotIOperator()
    {
        _now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", new System.Globalization.DateTimeFormatInfo());
    }

    public string combineBasepath(eProfilerMode profilerMode,string ip=null)
    {
        string mode="";
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
        return MemUtil.SnapshotsDir + "/" + _now + mode;
    }

    public void reset() { 
        saveSnapshotIndex=0;
        savePathIndex=0;
        isFristSave=true;
    }

    public string createPathDir(){
        string path =_getCurrentSnapshotPath();
        if (!Directory.Exists(path))
        {
            return _createNewDir();
        }
        else {
            if (isFristSave)
            {
                savePathIndex++;
                saveSnapshotIndex = 0;
                return createPathDir();
            }
            else
                return path;
        }
    }

    private string _getCurrentSnapshotPath() {
        string path;
        if (savePathIndex == 0)
        {
            path = _basePath + "/";
        }
        else
        {
            path = _basePath + "_" + savePathIndex + "/";
        }
        return path;
    }


    private string _createNewDir(){
        var temp = _getCurrentSnapshotPath();
        Directory.CreateDirectory(temp);
        isFristSave = false;
        return temp;
    }

    public bool saveAllSnapshot(List<MemSnapshotInfo> snapshotInfos,eProfilerMode profilerMode,string ip=null)
    {
        if (snapshotInfos.Count <= 0)
            return false;
        int count = 0;
        _basePath = combineBasepath(profilerMode,ip);
        var path = createPathDir();
        int index=0;
        foreach (var packed in snapshotInfos)
        {
            if (saveSnapshotIndex > index)
            {
                index++;
                continue;
            }
            string fileName = path + saveSnapshotIndex + ".memsnap";
            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    using (Stream stream = File.Open(fileName, FileMode.Create))
                    {
                        bf.Serialize(stream, packed);
                        saveSnapshotIndex++;
                        index++;
                        count++;
                    }
                }
                catch (Exception)
                {
                    DirectoryInfo TheFolder = new DirectoryInfo(path);
                    TheFolder.Delete();
                    return false; 
                }
            }
        }
        return true;
    }

    public bool loadSnapshotMemPacked(out System.Collections.Generic.List<object> result)
    {
        result =new List<object>();
        string pathName = EditorUtility.OpenFolderPanel("Load Snapshot Folder", MemUtil.SnapshotsDir, "");
        DirectoryInfo TheFolder = new DirectoryInfo(pathName);
        if (!TheFolder.Exists)
            return false;
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        foreach (var file in TheFolder.GetFiles())
        {
            var fileName = file.FullName;
            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    using (Stream stream = File.Open(fileName, FileMode.Open))
                    {
                        result.Add(bf.Deserialize(stream));
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        return true;
    }

}