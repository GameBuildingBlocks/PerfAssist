using System;
using System.Threading;
public  class StackInfoSynObj
{
    private bool readerNewMsgArrived = false;
    public bool ReaderNewMsgArrived
    {
        get { return readerNewMsgArrived; }
    }
    private string m_stackInfo;
    public string StackInfo
    {
        get { return m_stackInfo; }
    }
    public string readStackInfo()
    {
        if (m_stackInfo == null)
            return null;
        lock (this)
        {
            if (!readerNewMsgArrived)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch (SynchronizationLockException e)
                {
                    Console.WriteLine(e);
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
            }
            readerNewMsgArrived = false;
            Monitor.Pulse(this);
        }
        return m_stackInfo;
 	}

    public void writeStackInfo(string stackInfo)
    {
        lock (this)
        {
            if (readerNewMsgArrived)
            {
                try
                {
                    Monitor.Wait(this);
                }
                catch (SynchronizationLockException e)
                {
                    Console.WriteLine(e);
                }
                catch (ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
            }
            m_stackInfo = stackInfo;
            readerNewMsgArrived = true;
            Monitor.Pulse(this);
        }
    }
}