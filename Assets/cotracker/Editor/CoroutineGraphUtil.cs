using UnityEngine;
using System.Collections;

public class CoroutineGraphUtil 
{
    public static void LogData(string name, float value)
    {
        GraphIt.Log(name, value);
        GraphIt.StepGraph(name);
    }
}
