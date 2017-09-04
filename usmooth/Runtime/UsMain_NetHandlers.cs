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

using UnityEngine;
using System.Collections;
using System;

using System.Collections.Generic;

public class UsMain_NetHandlers
{
    public static UsMain_NetHandlers Instance;

    public UsMain_NetHandlers(UsCmdParsing exec)
    {
        exec.RegisterHandler(eNetCmd.CL_Handshake, NetHandle_Handshake);
        exec.RegisterHandler(eNetCmd.CL_KeepAlive, NetHandle_KeepAlive);
        exec.RegisterHandler(eNetCmd.CL_ExecCommand, NetHandle_ExecCommand);
        exec.RegisterHandler(eNetCmd.CL_RequestFrameData, NetHandle_RequestFrameData);
        exec.RegisterHandler(eNetCmd.CL_FrameV2_RequestMeshes, NetHandle_FrameV2_RequestMeshes);
        exec.RegisterHandler(eNetCmd.CL_FrameV2_RequestNames, NetHandle_FrameV2_RequestNames);
        exec.RegisterHandler(eNetCmd.CL_QuerySwitches, NetHandle_QuerySwitches);
        exec.RegisterHandler(eNetCmd.CL_QuerySliders, NetHandle_QuerySliders);
    }

    private bool NetHandle_Handshake(eNetCmd cmd, UsCmd c)
    {
        Debug.Log("executing handshake.");
        if (!string.IsNullOrEmpty(LogService.LastLogFile))
        {
            Debug.Log("Log Path: " + LogService.LastLogFile);
        }

        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_HandshakeResponse);
        UsNet.Instance.SendCommand(reply);
        return true;
    }

    private bool NetHandle_KeepAlive(eNetCmd cmd, UsCmd c)
    {
        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_KeepAliveResponse);
        UsNet.Instance.SendCommand(reply);
        return true;
    }

    private bool NetHandle_ExecCommand(eNetCmd cmd, UsCmd c)
    {
        string read = c.ReadString();
        bool ret = UsvConsole.Instance.ExecuteCommand(read);

        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_ExecCommandResponse);
        reply.WriteInt32(ret ? 1 : 0);
        UsNet.Instance.SendCommand(reply);
        return true;
    }

    private int SLICE_COUNT = 50;
    private bool NetHandle_RequestFrameData(eNetCmd cmd, UsCmd c)
    {
        if (usmooth.DataCollector.Instance == null)
            return true;

        FrameData data = usmooth.DataCollector.Instance.CollectFrameData();

        UsNet.Instance.SendCommand(data.CreatePacket());
        UsNet.Instance.SendCommand(usmooth.DataCollector.Instance.CreateMaterialCmd());
        UsNet.Instance.SendCommand(usmooth.DataCollector.Instance.CreateTextureCmd());

        UsCmd end = new UsCmd();
        end.WriteNetCmd(eNetCmd.SV_FrameDataEnd);
        UsNet.Instance.SendCommand(end);

        //Debug.Log(string.Format("creating frame packet: id {0} mesh count {1}", eNetCmd.SV_FrameDataV2, data._frameMeshes.Count));

        return true;
    }

    private bool NetHandle_FrameV2_RequestMeshes(eNetCmd cmd, UsCmd c)
    {
        if (usmooth.DataCollector.Instance != null)
        {
            List<int> meshIDs = UsCmdUtil.ReadIntList(c);
            //Debug.Log(string.Format("requesting meshes - count ({0})", meshIDs.Count));
            foreach (var slice in UsGeneric.Slice(meshIDs, SLICE_COUNT))
            {
                UsCmd fragment = new UsCmd();
                fragment.WriteNetCmd(eNetCmd.SV_FrameDataV2_Meshes);
                fragment.WriteInt32(slice.Count);
                foreach (int meshID in slice)
                {
                    usmooth.DataCollector.Instance.MeshTable.WriteMesh(meshID, fragment);
                }
                UsNet.Instance.SendCommand(fragment);
            }
        }

        return true;
    }

    private bool NetHandle_FrameV2_RequestNames(eNetCmd cmd, UsCmd c)
    {
        if (usmooth.DataCollector.Instance != null)
        {
            List<int> instIDs = UsCmdUtil.ReadIntList(c);
            foreach (var slice in UsGeneric.Slice(instIDs, SLICE_COUNT))
            {
                UsCmd fragment = new UsCmd();
                fragment.WriteNetCmd(eNetCmd.SV_FrameDataV2_Names);
                fragment.WriteInt32(slice.Count);
                foreach (int instID in slice)
                {
                    usmooth.DataCollector.Instance.WriteName(instID, fragment);
                }
                UsNet.Instance.SendCommand(fragment);
            }
        }

        return true;
    }

    private bool NetHandle_QuerySwitches(eNetCmd cmd, UsCmd c)
    {
        UsCmd pkt = new UsCmd();
        pkt.WriteNetCmd(eNetCmd.SV_QuerySwitchesResponse);

        pkt.WriteInt32(GameInterface.ObjectNames.Count);
        foreach (var name in GameInterface.ObjectNames)
        {
            //Log.Info("{0} {1} switch added.", name.Key, name.Value);
            pkt.WriteString(name.Key);
            pkt.WriteString(name.Value);
            pkt.WriteInt16(1);
        }
        UsNet.Instance.SendCommand(pkt);

        return true;
    }

    private bool NetHandle_QuerySliders(eNetCmd cmd, UsCmd c)
    {
        UsCmd pkt = new UsCmd();
        pkt.WriteNetCmd(eNetCmd.SV_QuerySlidersResponse);

        pkt.WriteInt32(GameInterface.VisiblePercentages.Count);
        foreach (var p in GameInterface.VisiblePercentages)
        {
            //Log.Info("{0} slider added.", p.Key);
            pkt.WriteString(p.Key);
            pkt.WriteFloat(0.0f);
            pkt.WriteFloat(100.0f);
            pkt.WriteFloat((float)p.Value);
        }
        UsNet.Instance.SendCommand(pkt);

        return true;
    }
}
