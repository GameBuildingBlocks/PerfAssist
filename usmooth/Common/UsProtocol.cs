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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum eNetCmd
{
    None,

    CL_CmdBegin = 1000,
    CL_Handshake,
    CL_KeepAlive,
    CL_ExecCommand,

    CL_RequestFrameData,
    CL_FrameV2_RequestMeshes,
    CL_FrameV2_RequestNames,

    CL_QuerySwitches,
    CL_QuerySliders,

    CL_RequestStackSummary,
    CL_StartAnalysePixels,

    CL_RequestStackData,

    CL_CmdEnd,

    SV_CmdBegin = 2000,
    SV_HandshakeResponse,
    SV_KeepAliveResponse,
    SV_ExecCommandResponse,

    SV_FrameDataV2,
    SV_FrameDataV2_Meshes,
    SV_FrameDataV2_Names,
    SV_FrameData_Material,
    SV_FrameData_Texture,
    SV_FrameDataEnd,

    SV_App_Logging,

    SV_QuerySwitchesResponse,
    SV_QuerySlidersResponse,
    SV_QueryStacksResponse,


    SV_VarTracerJsonParameter,


    SV_StressTestNames,
    SV_StressTestResult,

    SV_StartAnalysePixels,

    SV_CmdEnd,

    SV_SendLuaProfilerMsg,
    SV_StartLuaProfilerMsg,
}

public enum eSubCmd_TransmitStage
{
    DataBegin,
    DataSlice,
    DataEnd,
}
