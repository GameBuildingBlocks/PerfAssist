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

using System.Collections;
using System.Collections.Generic;


public class FrameData
{
    public int _frameCount = 0;

    // time info
    public float _frameDeltaTime = 0.0f;
    public float _frameRealTime = 0.0f;
    public float _frameStartTime = 0.0f;

    // actual data
    public List<int> _frameMeshes = new List<int>();
    public List<int> _frameMaterials = new List<int>();
    public List<int> _frameTextures = new List<int>();

    public UsCmd CreatePacket()
    {
        UsCmd cmd = new UsCmd();
        cmd.WriteNetCmd(eNetCmd.SV_FrameDataV2);
        cmd.WriteInt32(_frameCount);
        cmd.WriteFloat(_frameDeltaTime);
        cmd.WriteFloat(_frameRealTime);
        cmd.WriteFloat(_frameStartTime);
        UsCmdUtil.WriteIntList(cmd, _frameMeshes);
        UsCmdUtil.WriteIntList(cmd, _frameMaterials);
        UsCmdUtil.WriteIntList(cmd, _frameTextures);
        return cmd;
    }
}

public class MeshData
{
    public int _instID;
    public int _vertCount;
    public int _materialCount;
    public float _boundSize;
    public float _camDist;

    public void Write(UsCmd cmd)
    {
        cmd.WriteInt32(_instID);
        cmd.WriteInt32(_vertCount);
        cmd.WriteInt32(_materialCount);
        cmd.WriteFloat(_boundSize);
        cmd.WriteFloat(_camDist);
    }
}
