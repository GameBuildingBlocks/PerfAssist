using UnityEngine;
using System;

using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection;

public delegate void UsvClientDisconnectedHandler(string clientID);

public delegate bool UsvClientConsoleCmdHandler(string clientID, string[] args);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ClientConsoleCmdHandler : Attribute
{
    public ClientConsoleCmdHandler(string cmd)
    {
        Command = cmd;
    }

    public string Command;
}

public class UsvSimpleServer : IDisposable
{
    public event UsvClientDisconnectedHandler ClientDisconnected;

    private TcpListener _tcpListener;
    private Dictionary<string, TcpClient> _tcpClients = new Dictionary<string, TcpClient>();

    public UsCmdParsing CmdExecutor { get { return _cmdExec; } }
    private UsCmdParsing _cmdExec = new UsCmdParsing();

    public bool IsListening { get { return _isListening; } }
    bool _isListening = false;

    public UsvSimpleServer()
    {
        try
        {
            _cmdExec.RegisterClientHandler(eNetCmd.CL_Handshake, NetHandle_Handshake);
            _cmdExec.RegisterClientHandler(eNetCmd.CL_KeepAlive, NetHandle_KeepAlive);
            _cmdExec.RegisterClientHandler(eNetCmd.CL_ExecCommand, NetHandle_ExecCommand);

            _tcpListener = new TcpListener(IPAddress.Any, UsConst.SimpleServerPort);
            _tcpListener.Start();
            _tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, _tcpListener);

            NetLog("simple server listening started at: {0}.", UsConst.SimpleServerPort);

            _isListening = true;
        }
        catch (Exception e)
        {
            NetLog(e.ToString());
            throw;
        }
    }

    public void Dispose()
    {
        foreach (var client in _tcpClients.Values)
        {
            if (client != null)
            {
                NetLog(string.Format("Disconnecting client {0}.", client.Client.RemoteEndPoint));
                client.Close();
            }
        }
        _tcpClients.Clear();

        if (_tcpListener != null)
        {
            _tcpListener.Stop();
            _tcpListener = null;
            _isListening = false;

            NetLog("Listening ended.");
        }
    }

    List<string> _toBeRemoved = new List<string>();
    public void Update()
    {
        foreach (var p in _tcpClients)
        {
            var client = p.Value;
            try
            {
                while (client != null && client.Available > 0)
                {
                    byte[] cmdLenBuf = new byte[2];
                    int cmdLenRead = client.GetStream().Read(cmdLenBuf, 0, cmdLenBuf.Length);
                    ushort cmdLen = BitConverter.ToUInt16(cmdLenBuf, 0);
                    if (cmdLenRead > 0 && cmdLen > 0)
                    {
                        byte[] buffer = new byte[cmdLen];
                        int len = client.GetStream().Read(buffer, 0, buffer.Length);
                        if (len == buffer.Length)
                        {
                            // UsCmd c = new UsCmd(buffer);
                            // eNetCmd nc = c.ReadNetCmd();
                            // AddToLog(string.Format("cmd {0} - len: {1}", nc, len));

                            _cmdExec.ExecuteClient(p.Key, new UsCmd(buffer));
                        }
                        else
                        {
                            NetLog(string.Format("corrupted cmd received - len: {0}", len));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                client.Close();
                _toBeRemoved.Add(p.Key);

                if (ClientDisconnected != null)
                    ClientDisconnected(p.Key);
            }
        }

        foreach (var v in _toBeRemoved)
        {
            _tcpClients.Remove(v);
        }
    }

    public TcpClient FindClient(string clientID)
    {
        TcpClient client = null;
        if (!_tcpClients.TryGetValue(clientID, out client))
        {
            NetLog("unknown client: {0}", clientID);
            return null;
        }

        if (client == null || client.GetStream() == null)
        {
            NetLog("bad client: {0}", clientID);
            return null;
        }
        return client;
    }

    public void SendCommand(string clientID, UsCmd cmd)
    {
        TcpClient client = FindClient(clientID);
        if (client != null)
        {
            byte[] cmdLenBytes = BitConverter.GetBytes((ushort)cmd.WrittenLen);
            client.GetStream().Write(cmdLenBytes, 0, cmdLenBytes.Length);
            client.GetStream().Write(cmd.Buffer, 0, cmd.WrittenLen);
            //Debug.Log (string.Format("cmd written, len ({0})", cmd.WrittenLen));
        }
    }

    // Callback that gets called when a new incoming client
    // connection is established
    private void OnAcceptTcpClient(IAsyncResult asyncResult)
    {
        // Retrieve the TcpListener instance from IAsyncResult
        TcpListener listener = (TcpListener)asyncResult.AsyncState;
        if (listener == null)
            return;

        // Restart the connection accept procedure
        listener.BeginAcceptTcpClient(OnAcceptTcpClient, listener);

        try
        {
            // Retrieve newly connected TcpClient from IAsyncResult
            var client = listener.EndAcceptTcpClient(asyncResult);
            _tcpClients.Add(client.Client.RemoteEndPoint.ToString(), client);
            NetLog(string.Format("Client {0} connected.", client.Client.RemoteEndPoint));
        }
        catch (SocketException ex)
        {
            NetLog(string.Format("<color=red>Error accepting TCP connection: {0}</color>", ex.Message));
        }
        catch (ObjectDisposedException)
        {
            // The listener was Stop()'d, disposing the underlying socket and
            // triggering the completion of the callback. We're already exiting,
            // so just ignore this.
        }
        catch (Exception ex)
        {
            // Some other error occured. This should not happen
            Debug.LogException(ex);
            NetLog(string.Format("<color=red>An error occured: {0}</color>", ex.Message));
        }
    }

    private void NetLog(string text, params object[] args)
    {
        string formatted = args.Length > 0 ? string.Format(text, args) : text;
        Debug.LogFormat("<color=green>{0}</color>", formatted);
    }

    private void NetLogClient(string clientID, string text, params object[] args)
    {
        string formatted = args.Length > 0 ? string.Format(text, args) : text;
        Debug.LogFormat("<color=green>{0}</color> <color=white>{1}</color>", clientID, formatted);
    }

    private bool NetHandle_Handshake(string clientID, eNetCmd cmd, UsCmd c)
    {
        NetLog("executing handshake.");

        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_HandshakeResponse);
        SendCommand(clientID, reply);
        return true;
    }

    private bool NetHandle_KeepAlive(string clientID, eNetCmd cmd, UsCmd c)
    {
        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_KeepAliveResponse);
        SendCommand(clientID, reply);
        return true;
    }

    public void RegisterHandlerClass(Type handlerClassType, object handlerInst)
    {
        foreach (var method in handlerClassType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            foreach (var attr in method.GetCustomAttributes(typeof(ClientConsoleCmdHandler), false))
            {
                ClientConsoleCmdHandler handler = attr as ClientConsoleCmdHandler;
                if (handler != null)
                {
                    try
                    {
                        Delegate del = Delegate.CreateDelegate(typeof(UsvClientConsoleCmdHandler), handlerInst, method);
                        if (del != null)
                        {
                            string cmd = handler.Command.ToLower();
                            _clientConsoleCmdHandlers[cmd] = (UsvClientConsoleCmdHandler)del;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }
    }

    private Dictionary<string, UsvClientConsoleCmdHandler> _clientConsoleCmdHandlers = new Dictionary<string, UsvClientConsoleCmdHandler>();

    private bool NetHandle_ExecCommand(string clientID, eNetCmd cmd, UsCmd c)
    {
        string read = c.ReadString();

        string[] fragments = read.Split();
        if (fragments.Length == 0)
        {
            Log.Info("empty command received, ignored.");
            return false;
        }

        UsvClientConsoleCmdHandler handler;
        if (!_clientConsoleCmdHandlers.TryGetValue(fragments[0].ToLower(), out handler))
        {
            Log.Info("unknown command ('{0}') received, ignored.", read);
            return false;
        }

        bool succ = handler(clientID, fragments);

        UsCmd reply = new UsCmd();
        reply.WriteNetCmd(eNetCmd.SV_ExecCommandResponse);
        reply.WriteInt32(succ ? 1 : 0);
        SendCommand(clientID, reply);
        return true;
    }
}
