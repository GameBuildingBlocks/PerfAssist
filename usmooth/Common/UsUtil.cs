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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

public class SysPost
{
    public static bool AssertException(bool expr, string msg)
    {
#if DEBUG
        System.Diagnostics.Debug.Assert(expr);
        return expr;
#else
        if (expr)
            return true;

        throw new Exception(msg);
#endif
    }

    public delegate void StdMulticastDelegation(object sender, EventArgs e);

    public static void InvokeMulticast(object sender, MulticastDelegate md)
    {
        if (md != null)
        {
            InvokeMulticast(sender, md, null);
        }
    }

    public static void InvokeMulticast(object sender, MulticastDelegate md, EventArgs e)
    {
        if (md == null)
            return;

        foreach (StdMulticastDelegation Caster in md.GetInvocationList())
        {
            ISynchronizeInvoke SyncInvoke = Caster.Target as ISynchronizeInvoke;
            try
            {
                if (SyncInvoke != null && SyncInvoke.InvokeRequired)
                    SyncInvoke.Invoke(Caster, new object[] { sender, e });
                else
                    Caster(sender, e);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Event handling failed. \n");
                Console.WriteLine("{0}:\n", ex.ToString());
            }
        }
    }
}
