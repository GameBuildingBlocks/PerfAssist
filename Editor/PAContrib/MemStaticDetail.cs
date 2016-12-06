using UnityEngine;
using System.Collections;
using UnityEditor;

public enum StaticDetailTypes
{
    None,
    System,
    Mono,
    UnityEngine,
}


public class StaticDetailInfo
{
    public int systemStaticCount;
    public int systemStaticSize;

    public int monoStaticCount;
    public int monoStaticSize;

    public int UnityEngineStaticCount;
    public int UnityEngineStaticSize;

    public int wholeCount;
    public int wholeSize;

    public void clear() {
        systemStaticCount = 0;
        systemStaticSize = 0;

        monoStaticCount = 0;
        monoStaticSize = 0;

        UnityEngineStaticCount = 0;
        UnityEngineStaticSize = 0;

        wholeCount = 0;
        wholeSize = 0;
    }

    public StaticDetailTypes checkStaticDetailType(string str) { 
        if(str.StartsWith("static fields of system."))
        {
                return StaticDetailTypes.System;
        }
            
        if(str.StartsWith("static fields of mono."))
        {
                return StaticDetailTypes.Mono;
        }

        if(str.StartsWith("static fields of unityengine."))
        {
                return StaticDetailTypes.UnityEngine;
        }
        return StaticDetailTypes.None;
    }

    public void statisticsDetail(StaticDetailTypes types,int Size) {
        switch (types)
        {
            case StaticDetailTypes.System:
                this.systemStaticSize += Size;
                this.systemStaticCount++;
                break;
            case StaticDetailTypes.Mono:
                this.monoStaticSize += Size;
                this.monoStaticCount++;
                break;
            case StaticDetailTypes.UnityEngine:
                this.UnityEngineStaticSize += Size;
                this.UnityEngineStaticCount++;
                break;
            default:
                return;
        }
        this.wholeCount++;
        this.wholeSize += Size;
    }


    public bool isDetailStaticFileds(string typeName,string name,int size) {
        if (!typeName.Equals("StaticFields"))
            return false;
        var lowerStr = name.ToLower();
        var detailType =checkStaticDetailType(lowerStr);
        if (detailType==StaticDetailTypes.None)    
            return false;
        statisticsDetail(detailType,size);
        return true;
    }


    public void showInfos(){
        EditorUtility.DisplayDialog("staticField 详细统计信息",
            string.Format("由系统持有的[static]变量类型统计\n\nSystem. Count ：{0}  | Size ：{1} \n\nMono. Count ：{2}  | Size：{3} \n\nUnityEngine. Count ：{4}  | Size:{5} \n\nTotal Count ：{6}  | Size：{7}",
            systemStaticCount, EditorUtility.FormatBytes(systemStaticSize), monoStaticCount, EditorUtility.FormatBytes(monoStaticSize)
            , UnityEngineStaticCount, EditorUtility.FormatBytes(UnityEngineStaticSize), wholeCount, EditorUtility.FormatBytes(wholeSize)), "确认");
    }
}


