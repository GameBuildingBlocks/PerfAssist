using UnityEngine;
using System.Collections;

public class SampleRunner : MonoBehaviour {

    void Start()
    {
        // bootstrapping
        RuntimeCoroutineTracker.EnableTrackingSystemProgrammatically();
        StartCoroutine(RuntimeCoroutineTracker.DefaultStatsReportCoroutine());

        var spawner = gameObject.GetComponent<CoroutineSpawner>() as CoroutineSpawner;

        RuntimeCoroutineTracker.InvokeStart(spawner, "Co01_WaitForSeconds");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co02_PerFrame_NULL");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co03_PerFrame_EOF");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co04_PerFrame_ARG", 0.683f);
    }

    void Update()
    {

    }
}
