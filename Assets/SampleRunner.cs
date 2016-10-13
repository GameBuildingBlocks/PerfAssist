using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SampleRunner : MonoBehaviour {

    void Start()
    {
        // bootstrapping
        CoroutineRuntimeTrackingConfig.EnableTracking = true;
        StartCoroutine(RuntimeCoroutineStats.Instance.BroadcastCoroutine());
#if UNITY_EDITOR
        EditorWindow w = EditorWindow.GetWindow<EditorWindow>("CoroutineTrackerWindow");
        if (w.GetType().Name == "CoroutineTrackerWindow")
        {
            w.SendEvent(EditorGUIUtility.CommandEvent("AppStarted"));
        }
#endif

        gameObject.AddComponent<TestPluginRunner>();

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
    }
}
