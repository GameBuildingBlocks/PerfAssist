using UnityEngine;
using System.Collections;

public class SampleRunner : MonoBehaviour {

    void Start()
    {
        // bootstrapping
        RuntimeCoroutineTracker.EnableTrackingSystemProgrammatically();
        StartCoroutine(RuntimeCoroutineTracker.DefaultStatsReportCoroutine());

        TestPluginRunner pluginRunner = gameObject.AddComponent<TestPluginRunner>();
        CoroutinePluginForwarder.InvokeStart_IEnumerator = RuntimeCoroutineTracker.InvokeStart;
        CoroutinePluginForwarder.InvokeStart_String = RuntimeCoroutineTracker.InvokeStart;

        CoroutineSpawner spawner = gameObject.AddComponent<CoroutineSpawner>();
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co01_WaitForSeconds");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co02_PerFrame_NULL");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co03_PerFrame_EOF");
        RuntimeCoroutineTracker.InvokeStart(spawner, "Co04_PerFrame_ARG", 0.683f);
    }

    void Update()
    {
        GraphIt.Log("Master", "sub1", Random.value * 5.0f);
        GraphIt.Log("Master", "sub2", Random.value * 15.0f);
    }
}
