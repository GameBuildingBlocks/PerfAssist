using UnityEngine;
using System.Collections;

public class CoroutineActivity
{
    public int seqID;
    public float timestamp;
    public int curFrame;
    public string typeName; // 仅作为方便解析json使用

    public CoroutineActivity(int id)
    {
        seqID = id;
        timestamp = Time.realtimeSinceStartup;
        typeName = GetType().Name.Substring("Coroutine".Length);
    }
}

public class CoroutineCreation : CoroutineActivity
{
    public string mangledName;
    public string stacktrace;

    public CoroutineCreation(int seq) : base(seq)
    {
    }
}

public class CoroutineExecution : CoroutineActivity
{
    public float timeConsumed;

    public CoroutineExecution(int seq) : base(seq)
    {
    }
}

public class CoroutineTermination : CoroutineActivity
{
    public CoroutineTermination(int seq) : base(seq)
    {
    }
}
