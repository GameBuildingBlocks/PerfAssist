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


// This is copied from UnityEngine.dll to keep compatible with it 
public enum UsLogType 
{
    Error = 0,
    Assert = 1,
    Warning = 2,
    Log = 3,
    Exception = 4,
}

public class UsLogPacket
{
    #region Constants
    public const int MAX_CONTENT_LEN = 1024;
    public const int MAX_CALLSTACK_LEN = 1024;
    #endregion

    // main info
    public ushort SeqID;
    public UsLogType LogType;
    public string Content;

    // time info
    public float RealtimeSinceStartup;

    // debugging info
    public string Callstack;

    public UsLogPacket() 
    {
        SeqID = ushort.MaxValue;
    }

    public UsLogPacket(UsCmd c)
    {
        SeqID = (ushort)c.ReadInt16();
        LogType = (UsLogType)c.ReadInt32();
        Content = c.ReadString();
        RealtimeSinceStartup = c.ReadFloat();
    }

    public UsCmd CreatePacket()
    {
        UsCmd c = new UsCmd();
        c.WriteNetCmd(eNetCmd.SV_App_Logging);
        c.WriteInt16((short)SeqID);
        c.WriteInt32((int)LogType);
        c.WriteStringStripped(Content, MAX_CONTENT_LEN);
        c.WriteFloat(RealtimeSinceStartup);
        return c;
    }
}
