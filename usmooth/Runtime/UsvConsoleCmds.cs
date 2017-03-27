/*!lic_info

The MIT License (MIT)

Copyright (c) 2015 SeaSunOpenSource

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/

﻿using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ConsoleHandler : Attribute
{
    public ConsoleHandler(string cmd)
    {
        Command = cmd;
    }

    public string Command;
}

public class UsvConsoleCmds 
{
	public static UsvConsoleCmds Instance;

    [ConsoleHandler("showmesh")]
    public bool ShowMesh(string[] args)
    {
        return SetMeshVisible(args[1], true);
    }

    [ConsoleHandler("hidemesh")]
    public bool HideMesh(string[] args)
    {
        return SetMeshVisible(args[1], false);
    }

    private bool SetMeshVisible(string strInstID, bool visible)
    {
        int instID = 0;
        if (!int.TryParse(strInstID, out instID))
            return false;

        MeshRenderer[] meshRenderers = UnityEngine.Object.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
        foreach (MeshRenderer mr in meshRenderers)
        {
            if (mr.gameObject.GetInstanceID() == instID)
            {
                mr.enabled = visible;
                return true;
            }
        }

        return false;
    }

    [ConsoleHandler("testlogs")]
    public bool PrintTestLogs(string[] args)
    {
        Debug.Log("A typical line of logging.");
        Debug.Log("Another line.");
        Debug.LogWarning("An ordinary warning.");
        Debug.LogError("An ordinary error.");

        Debug.Log("misc.中.文.测.试.test.中文测试.");

        try
        {
            throw new ApplicationException("A user-thrown exception.");
        }
        catch (ApplicationException ex)
        {
            Debug.LogException(ex);
        }

        return true;
    }

    [ConsoleHandler("toggle")]
    public bool ToggleSwitch(string[] args)
    {
        try
        {
            GameInterface.Instance.ToggleSwitch(args[1], int.Parse(args[2]) != 0);
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        return true;
    }

    [ConsoleHandler("slide")]
    public bool SlideChanged(string[] args)
    {
        try
        {
            GameInterface.Instance.ChangePercentage(args[1], double.Parse(args[2]));
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        return true;
    }


    [ConsoleHandler("flyto")]
    public bool FlyTo(string[] args)
    {
        try
        {
#if UNITY_EDITOR
            int instID = int.Parse(args[1]);
            MeshRenderer[] meshRenderers = UnityEngine.Object.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
            foreach (MeshRenderer mr in meshRenderers)
            {
                if (mr.isVisible && mr.gameObject.GetInstanceID() == instID)
                {
                    if (SceneView.currentDrawingSceneView != null)
                    {
                        SceneView.currentDrawingSceneView.LookAt(mr.gameObject.transform.position);
                    }

                    Selection.activeGameObject = mr.gameObject;
                    break;
                }
            }
#endif
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        return true;
    }

    public event SysPost.StdMulticastDelegation QueryEffectList;
    public event SysPost.StdMulticastDelegation RunEffectStressTest;

    public event SysPost.StdMulticastDelegation StartAnalyzePixel;

    public class AnalysePixelsArgs : EventArgs
    {
        public AnalysePixelsArgs(bool b)
        {
            bRefresh = b;
        }
        public bool bRefresh;
    }

    [ConsoleHandler("start_analyze_pixels")]
    public bool StartAnalysePixelsTriggered(string[] args)
    {
        try
        {
            bool bRefresh = false;
            if (args.Length == 2)
            {
                string args2 = args[1];
                if (args2 == "refresh")
                { 
                    bRefresh = true;
                }
            }

            SysPost.InvokeMulticast(this, StartAnalyzePixel, new AnalysePixelsArgs(bRefresh));
        }
        catch(Exception ex)
        {
            Log.Exception(ex);
            throw;
        }

        return true;
    }


    [ConsoleHandler("get_effect_list")]
    public bool QueryEffectListTriggered(string[] args)
    {
        try
        {
            if (args.Length != 1)
            {
                Log.Error("Command 'get_effect_list' parameter count mismatched. ({0} expected, {1} got)", 1, args.Length);
                return false;
            }

            SysPost.InvokeMulticast(this, QueryEffectList);
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        return true;
    }

    public class UsEffectStressTestEventArgs : EventArgs
    {
        public UsEffectStressTestEventArgs(string effectName, int effectCount)
        {
            _effectName = effectName;
            _effectCount = effectCount;
        }

        public string _effectName;
        public int _effectCount = 0;
    }

    [ConsoleHandler("run_effect_stress")]
    public bool EffectStressTestTriggered(string[] args)
    {
        try
        {
            if (args.Length == 3)
            {
                string effectName = args[1];
                int effectCount = int.Parse(args[2]);
                SysPost.InvokeMulticast(this, RunEffectStressTest, new UsEffectStressTestEventArgs(effectName, effectCount));
            }
            else
            {
                int effectCount = int.Parse(args[args.Length - 1]);
                List<string> effectNameList = new List<string>(args);
                effectNameList.RemoveAt(0);
                effectNameList.RemoveAt(effectNameList.Count - 1);
                string effectName = string.Join(" ", effectNameList.ToArray());
                SysPost.InvokeMulticast(this, RunEffectStressTest, new UsEffectStressTestEventArgs(effectName, effectCount));
            }
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
        return true;
    }
}
