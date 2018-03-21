using UnityEngine;
using System.Collections;

public class CoGraphUtil 
{
    public static string GName_Creation = "creation_count";
    public static string GName_Termination = "termination_count";
    public static string GName_ExecCount = "execution_count";
    public static string GName_ExecTime = "exection_time";

    static public float[] mSnapshotTimestamps = new float[GraphItData.DEFAULT_SAMPLES];

    public static void InitParams(string name, float height, Color color)
    {
        GraphIt.GraphSetupHeight(name, height);
        GraphIt.GraphSetupColour(name, color);
    }
    public static void LogData(string name, float value)
    {
        GraphIt.Log(name, value);
        GraphIt.StepGraph(name);
    }

    // 这个函数一定要在 Step 之前调用（否则 mCurrentIndex 就变到下一帧去了）
    public static void RecordSnapshot(float snapshotTime)
    {
        GraphItData g = GraphIt.Instance.Graphs[GName_Creation];
        if (g == null)
            return;

        mSnapshotTimestamps[g.mCurrentIndex] = snapshotTime;
    }

    public static float GetSnapshotTime(int index)
    {
        return mSnapshotTimestamps[index];
    }
}
