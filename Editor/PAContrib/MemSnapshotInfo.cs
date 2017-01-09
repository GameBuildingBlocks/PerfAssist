using UnityEngine;
using System.Collections;
using MemoryProfilerWindow;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.MemoryProfiler;

public class MemSnapshotInfo 
{
    public CrawledMemorySnapshot unPacked=null;
    public float snapshotTime = 0;
 	public int dealtaSize =0;
    public List<ThingInMemory> addedList = new List<ThingInMemory>();
    public List<ThingInMemory> removedList = new List<ThingInMemory>();

    public void setSnapShotTime(float time){
        snapshotTime = time;
    }

    public string showDealtaSizeStr() {
       return EditorUtility.FormatBytes(_calculateDealtaSize()); 
    }

    public int _calculateDealtaSize() {
        int resultSize = 0;
        foreach (ThingInMemory thingInMemory in addedList)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;
            resultSize += thingInMemory.size;
        }

        foreach (ThingInMemory thingInMemory in removedList)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;
            resultSize += thingInMemory.size;
        }
        dealtaSize = resultSize;
        return resultSize;
    }

    public void calculateDealta() { 
        //计算addLsit,removeList
            
    }

}

