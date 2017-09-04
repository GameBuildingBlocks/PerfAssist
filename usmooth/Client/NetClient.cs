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
using System.IO;
using System.Net.Sockets;

public class NetClient : IDisposable
{
    public event SysPost.StdMulticastDelegation Connected;
    public event SysPost.StdMulticastDelegation Disconnected;

    public bool IsConnected { get { return _tcpClient != null; } }

    public string RemoteAddr { get { return IsConnected ? _tcpClient.Client.RemoteEndPoint.ToString() : ""; } }

    public void Connect(string host, int port)
    {
        _host = host;
        _port = port;
        _tcpClient = new TcpClient();
        _tcpClient.BeginConnect(_host, _port, OnConnect, _tcpClient);
        NetUtil.Log("connecting to [u]{0}:{1}[/u]...", host, port);
    }

    public void Disconnect()
    {
        if (_tcpClient != null)
        {
            _tcpClient.Close();
            _tcpClient = null;

            _host = "";
            _port = 0;

            NetUtil.Log("connection closed.");
            SysPost.InvokeMulticast(this, Disconnected);
        }
    }

    public void RegisterCmdHandler(eNetCmd cmd, UsCmdHandler handler)
    {
        _cmdParser.RegisterHandler(cmd, handler);
    }

    public void Tick_CheckConnectionStatus()
    {
        try
        {
            if (!_tcpClient.Connected)
            {
                NetUtil.Log("disconnection detected. (_tcpClient.Connected == false).");
                throw new Exception();
            }

            // check if the client socket is still readable
            if (_tcpClient.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] checkConn = new byte[1];
                if (_tcpClient.Client.Receive(checkConn, SocketFlags.Peek) == 0)
                {
                    NetUtil.Log("disconnection detected. (failed to read by Poll/Receive).");
                    throw new IOException();
                }
            }
        }
        catch (Exception ex)
        {
            DisconnectOnError("disconnection detected while checking connection status.", ex);
        }
    }

    public void Tick_ReceivingData()
    {
        try
        {
            while (_tcpClient.Available > 0)
            {
                byte[] cmdLenBuf = new byte[2];
                int cmdLenRead = _tcpClient.GetStream().Read(cmdLenBuf, 0, cmdLenBuf.Length);
                ushort cmdLen = BitConverter.ToUInt16(cmdLenBuf, 0);
                if (cmdLenRead > 0 && cmdLen > 0)
                {
                    byte[] buffer = new byte[cmdLen];
                    int len = _tcpClient.GetStream().Read(buffer, 0, buffer.Length);

                    UsCmd cmd = new UsCmd(buffer);
                    UsCmdExecResult result = _cmdParser.Execute(cmd);
                    switch (result)
                    {
                    case UsCmdExecResult.Succ:
                        break;
                    case UsCmdExecResult.Failed:
                        NetUtil.Log("net cmd execution failed: {0}.", new UsCmd(buffer).ReadNetCmd());
                        break;
                    case UsCmdExecResult.HandlerNotFound:
                        NetUtil.Log("net unknown cmd: {0}.", new UsCmd(buffer).ReadNetCmd());
                        break;
                    }

                    len++; // warning CS0219: The variable `len' is assigned but its value is never used
                }
            }
        }
        catch (Exception ex)
        {
            DisconnectOnError("error detected while receiving data.", ex);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }

    public void SendPacket(UsCmd cmd)
    {
        try
        {
            byte[] cmdLenBytes = BitConverter.GetBytes((ushort)cmd.WrittenLen);
            _tcpClient.GetStream().Write(cmdLenBytes, 0, cmdLenBytes.Length);
            _tcpClient.GetStream().Write(cmd.Buffer, 0, cmd.WrittenLen);
        }
        catch (Exception ex)
        {
            DisconnectOnError("error detected while sending data.", ex);
        }
    }

    // Called when a connection to a server is established
    private void OnConnect(IAsyncResult asyncResult)
    {
        // Retrieving TcpClient from IAsyncResult
        TcpClient tcpClient = (TcpClient)asyncResult.AsyncState;

        try
        {
            if (tcpClient.Connected) // may throw NullReference
            {
                NetUtil.Log("connected successfully.");
                SysPost.InvokeMulticast(this, Connected);
            }
            else
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            DisconnectOnError("connection failed while handling OnConnect().", ex);
        }
    }

    private void DisconnectOnError(string info, Exception ex)
    {
        NetUtil.Log(info);
        NetUtil.Log(ex.ToString());

        Disconnect();
    }

    private string _host = "";
    private int _port = 0;
    private TcpClient _tcpClient;
    private UsCmdParsing _cmdParser = new UsCmdParsing();
}
