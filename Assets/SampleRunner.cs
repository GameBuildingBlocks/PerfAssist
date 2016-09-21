using UnityEngine;
using System.Collections;

public class SampleRunner : MonoBehaviour {

    void Start()
    {
        // bootstrapping
        RuntimeCoroutineTracker.EnableTrackingSystemProgrammatically();
        StartCoroutine(RuntimeCoroutineTracker.DefaultStatsReportCoroutine());

        Debug.LogFormat("start: {0}", Time.time);
        RuntimeCoroutineTracker.InvokeStart(this, "TargetCoroutine");
        Debug.LogFormat("start: {0} (post)", Time.time);
    }

    void Update()
    {

    }

    private IEnumerator TargetCoroutine()
    {
        while (true)
        {
            float currentTime = Time.time;
            if (currentTime > 1.0f)
                break;

            Debug.LogFormat("next: {0}", currentTime);
            yield return new WaitForSeconds(0.3f);
        }
        Debug.LogFormat("last: {0}", Time.time);
    }
}
