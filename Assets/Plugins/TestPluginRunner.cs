using UnityEngine;
using System.Collections;

public class TestPluginRunner : MonoBehaviour
{
	// Use this for initialization
	void Start () {
        CoroutinePluginForwarder.InvokeStart(this, Co01_Plugin_NULL());
        CoroutinePluginForwarder.InvokeStart(this, "Co02_Plugin_EOF_ByName");
        CoroutinePluginForwarder.InvokeStart(this, "Co03_Plugin_EOF_ByNameWithArg", 0.369f);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public IEnumerator Co01_Plugin_NULL()
    {
        while (true)
        {
            Debug.LogFormat("Co01_Plugin_NULL.tick: {0}", Time.time);
            yield return null;
        }
    }
    public IEnumerator Co02_Plugin_EOF_ByName()
    {
        while (true)
        {
            Debug.LogFormat("Co02_Plugin_EOF_ByName.tick: {0}", Time.time);
            yield return new WaitForEndOfFrame();
        }
    }
    public IEnumerator Co03_Plugin_EOF_ByNameWithArg(float arg)
    {
        while (true)
        {
            Debug.LogFormat("Co03_Plugin_EOF_ByNameWithArg.tick: {0} - {1}", Time.time, arg);
            yield return new WaitForEndOfFrame();
        }
    }
}
