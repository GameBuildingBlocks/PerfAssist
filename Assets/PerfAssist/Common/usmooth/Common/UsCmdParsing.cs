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


using System.Collections.Generic;

using System;

public delegate bool UsCmdHandler(eNetCmd cmd, UsCmd c);
public delegate bool UsClientCmdHandler(string clientID, eNetCmd cmd, UsCmd c);

public enum UsCmdExecResult
{
    Succ,
    Failed,
    HandlerNotFound,
}

public class UsCmdParsing
{
    public void RegisterHandler(eNetCmd cmd, UsCmdHandler handler)
    {
        m_handlers[cmd] = handler;
    }

    public void RegisterClientHandler(eNetCmd cmd, UsClientCmdHandler handler)
    {
        m_clientHandlers[cmd] = handler;
    }

    public UsCmdExecResult Execute(UsCmd c)
    {
        try
        {
            eNetCmd cmd = c.ReadNetCmd();
            UsCmdHandler handler;
            if (!m_handlers.TryGetValue(cmd, out handler))
            {
                return UsCmdExecResult.HandlerNotFound;
            }

            if (handler(cmd, c))
            {
                return UsCmdExecResult.Succ;
            }
            else
            {
                return UsCmdExecResult.Failed;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[cmd] Execution failed. ({0})", ex.Message);
            return UsCmdExecResult.Failed;
        }
    }
    public UsCmdExecResult ExecuteClient(string clientID, UsCmd c)
    {
        try
        {
            eNetCmd cmd = c.ReadNetCmd();
            UsClientCmdHandler handler;
            if (!m_clientHandlers.TryGetValue(cmd, out handler))
            {
                return UsCmdExecResult.HandlerNotFound;
            }

            if (handler(clientID, cmd, c))
            {
                return UsCmdExecResult.Succ;
            }
            else
            {
                return UsCmdExecResult.Failed;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[cmd] Execution failed. ({0})", ex.Message);
            return UsCmdExecResult.Failed;
        }
    }


    Dictionary<eNetCmd, UsCmdHandler> m_handlers = new Dictionary<eNetCmd, UsCmdHandler>();
    Dictionary<eNetCmd, UsClientCmdHandler> m_clientHandlers = new Dictionary<eNetCmd, UsClientCmdHandler>();
}
