using UnityEngine;
using System.Collections;
using UnityEditor.MemoryProfiler;
using System.IO;
using System;
using UnityEditor;
using System.Collections.Generic;
public class SnapshotIOperator {
    public int saveSnapshotIndex=0;
    public int savePathIndex=0;
    public bool isFristSave=true;
    public string now;
    public string basePath;

    public SnapshotIOperator() {
        now = DateTime.Now.ToString("ddddMMMMddyyyy", new System.Globalization.DateTimeFormatInfo());
        basePath = MemUtil.SnapshotsDir + "/" + now;
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
            path = basePath + "/";
        }
        else
        {
            path = basePath + "_" + savePathIndex + "/";
        }
        return path;
    }


    private string _createNewDir(){
        var temp = _getCurrentSnapshotPath();
        Directory.CreateDirectory(temp);
        isFristSave = false;
        return temp;
    }

    public void saveAllSnapshot(List<PackedMemorySnapshot> snapshots)
    {
        if (snapshots.Count <= 0)
            return;
        int count = 0;
        var path = createPathDir();
        int index=0;
        foreach (var packed in snapshots)
        {
            if (saveSnapshotIndex > index)
            {
                index++;
                continue;
            }
            string fileName = path + saveSnapshotIndex + ".memsnap";
            if (!string.IsNullOrEmpty(fileName))
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
        }
        EditorUtility.DisplayDialog("save all snapshots",
            string.Format("\nsave all snapshots successed!, save  {0} snapshots \n\n\nSavepath:{1}", count, path), "confirm");
    }

    public System.Collections.Generic.List<object> loadSnapshotMemPacked()
    {
        List<object> result =new List<object>();
        string pathName = EditorUtility.OpenFolderPanel("Load Snapshot Folder", MemUtil.SnapshotsDir, "");
        DirectoryInfo TheFolder = new DirectoryInfo(pathName);
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        foreach (var file in TheFolder.GetFiles())
        {
            var fileName = file.FullName;
            if (!string.IsNullOrEmpty(fileName))
            {
                using (Stream stream = File.Open(fileName, FileMode.Open))
                {
                    result.Add(bf.Deserialize(stream));
                }
            }
        }
        return result;
    }
}