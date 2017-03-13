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

﻿using UnityEngine;
using System;

using System.Net;
using System.Net.Sockets;

public class UsNet : IDisposable {

	public static UsNet Instance;
	
	// TcpListener instance, encapsulating 
	// typical socket server interactions
	private TcpListener _tcpListener;

	private TcpClient _tcpClient;

    private readonly object _netLocker = new object();

	public UsCmdParsing CmdExecutor { get { return _cmdExec; } }
	private UsCmdParsing _cmdExec = new UsCmdParsing();
    public bool IsListening { get { return _isListening; } }
    bool _isListening = false;
	// QOTD server constructor
	public UsNet()
    {
		try
        {
			// Create a listening server that accepts connections from
			// any addresses on a given port
            _tcpListener = new TcpListener(IPAddress.Any, UsConst.ServerPort);
			// Switch the listener to a started state
			_tcpListener.Start();
			// Set the callback that'll be called when a client connects to the server
			_tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, _tcpListener);

            AddToLog("usmooth listening started at: {0}.", UsConst.ServerPort);

            _isListening = true;
        }
        catch (Exception e)
        {
            AddToLog(e.ToString());
			throw;
		}
	}
	
	~UsNet() {
		FreeResources();
	}
	
	// Free the resources
	public void Dispose() {
		
		CloseTcpClient ();
		
		if (_tcpListener != null) {
			FreeResources();
			
			AddToLog("Listening canceled.");
		}
	}
	
	private void FreeResources() {
		
		if (_tcpListener != null) {
			_tcpListener.Stop();
			_tcpListener = null;
            _isListening = false;
		}
	}
	
	private void CloseTcpClient() {
		if (_tcpClient != null) {
			AddToLog(string.Format("Disconnecting client {0}.", _tcpClient.Client.RemoteEndPoint));
			_tcpClient.Close();
			_tcpClient = null;
		}
	}

	public void Update() {
		if (_tcpClient == null) {
			return;
		}
		
		try {
			while (_tcpClient.Available > 0) {
				byte[] cmdLenBuf = new byte[2];
				int cmdLenRead = _tcpClient.GetStream().Read(cmdLenBuf, 0, cmdLenBuf.Length);
				ushort cmdLen = BitConverter.ToUInt16(cmdLenBuf, 0);
				if (cmdLenRead > 0 && cmdLen > 0) {
					byte[] buffer = new byte[cmdLen];
					int len = _tcpClient.GetStream().Read(buffer, 0, buffer.Length);
					if (len == buffer.Length) {
//						UsCmd c = new UsCmd(buffer);
//					    eNetCmd nc = c.ReadNetCmd();
//						AddToLog(string.Format("cmd {0} - len: {1}", nc, len));

						_cmdExec.Execute(new UsCmd(buffer));
					} else {
						AddToLog(string.Format("corrupted cmd received - len: {0}", len));
					}
				}
			}
		} catch (Exception ex) {
			Debug.LogException(ex);
			CloseTcpClient();
		}
	}

	public void SendCommand(UsCmd cmd) {
        if (_tcpClient == null || _tcpClient.GetStream() == null)
        {
            return;
        }
        lock (_netLocker)
        {
            byte[] cmdLenBytes = BitConverter.GetBytes((ushort)cmd.WrittenLen);
            _tcpClient.GetStream().Write(cmdLenBytes, 0, cmdLenBytes.Length);
            _tcpClient.GetStream().Write(cmd.Buffer, 0, cmd.WrittenLen);
        }
		//Debug.Log (string.Format("cmd written, len ({0})", cmd.WrittenLen));
	}

	// Callback that gets called when a new incoming client
	// connection is established
	private void OnAcceptTcpClient(IAsyncResult asyncResult) {
		// Retrieve the TcpListener instance from IAsyncResult
		TcpListener listener = (TcpListener) asyncResult.AsyncState;
		if (listener == null)
			return;
		
		// Restart the connection accept procedure
		listener.BeginAcceptTcpClient(OnAcceptTcpClient, listener);
		
		try {
			// Retrieve newly connected TcpClient from IAsyncResult
			_tcpClient = listener.EndAcceptTcpClient(asyncResult);
			AddToLog(string.Format("Client {0} connected.", _tcpClient.Client.RemoteEndPoint));
		} catch (SocketException ex) {
			AddToLog(string.Format("<color=red>Error accepting TCP connection: {0}</color>", ex.Message));
		} catch (ObjectDisposedException) {
			// The listener was Stop()'d, disposing the underlying socket and
			// triggering the completion of the callback. We're already exiting,
			// so just ignore this.
		} catch (Exception ex) {
			// Some other error occured. This should not happen
			Debug.LogException(ex);
			AddToLog(string.Format("<color=red>An error occured: {0}</color>", ex.Message));
		}
	}

	// Adds a formatted entry to the log
	private void AddToLog(string text, params object[] args) {
        string formatted = args.Length > 0 ? string.Format(text, args) : text;
		if (_tcpClient != null) {
			Debug.LogFormat("<color=green>{0}</color> <color=white>{1}</color>", _tcpClient.Client.RemoteEndPoint, formatted);
		} else {
			Debug.LogFormat("<color=green>{0}</color>", formatted);
		}
	}
}

