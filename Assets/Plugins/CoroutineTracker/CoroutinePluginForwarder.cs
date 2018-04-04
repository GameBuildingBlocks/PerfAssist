using UnityEngine;
using System.Collections;

public delegate Coroutine CoroutineStartHandler_IEnumerator(MonoBehaviour initiator, IEnumerator routine);
public delegate Coroutine CoroutineStartHandler_String(MonoBehaviour initiator, string methodName, object arg = null);

public class CoroutinePluginForwarder
{
    public static CoroutineStartHandler_IEnumerator InvokeStart_IEnumerator;
    public static CoroutineStartHandler_String InvokeStart_String;

    public static Coroutine InvokeStart(MonoBehaviour initiator, IEnumerator routine)
    {
        if (InvokeStart_String != null)
        {
            return InvokeStart_IEnumerator(initiator, routine);
        }
        else
        {
            return initiator.StartCoroutine(routine);
        }
    }

    public static Coroutine InvokeStart(MonoBehaviour initiator, string methodName, object arg = null)
    {
        if (InvokeStart_String != null)
        {
            return InvokeStart_String(initiator, methodName, arg);
        }
        else
        {
            return initiator.StartCoroutine(methodName, arg);
        }
    }
}
