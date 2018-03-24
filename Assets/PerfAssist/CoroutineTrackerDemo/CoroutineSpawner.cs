using UnityEngine;
using System.Collections;

public class CoroutineSpawner : MonoBehaviour
{
    public IEnumerator Co01_WaitForSeconds()
    {
        while (true)
        {
            Debug.LogFormat("Co01_WaitForSeconds: {0}", Time.time);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public IEnumerator Co02_PerFrame_NULL()
    {
        while (true)
        {
            Debug.LogFormat("Co02_PerFrame_NULL.tick: {0}", Time.time);
            yield return null;
        }
    }
    public IEnumerator Co03_PerFrame_EOF()
    {
        while (true)
        {
            Debug.LogFormat("Co03_PerFrame_EOF.tick: {0}", Time.time);
            yield return new WaitForEndOfFrame();
        }
    }
    public IEnumerator Co04_PerFrame_ARG(float argFloat)
    {
        while (true)
        {
            Debug.LogFormat("Co04_PerFrame_ARG.tick: {0} - {1}", Time.time, argFloat);
            yield return new WaitForEndOfFrame();
        }
    }
}
