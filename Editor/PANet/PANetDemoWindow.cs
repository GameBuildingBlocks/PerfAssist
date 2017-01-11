using UnityEngine;
using System.Collections;
using UnityEditor;

public class PANetDemoWindow : EditorWindow
{
    [MenuItem(PAEditorConst.DemoTestPath + "/PANet Demo")]
    static void Create()
    {
        EditorWindow.GetWindow<PANetDemoWindow>();
    }

    void OnEnable()
    {
        if (PANetDrv.Instance == null)
        {
            PANetDrv.Instance = new PANetDrv();
            Debug.LogErrorFormat("PANetDrv is not available.");
        }
    }

    void OnDisable()
    {
        if (PANetDrv.Instance != null)
        {
            PANetDrv.Instance.Dispose();
            PANetDrv.Instance = null;
        }
    }

    void OnGUI()
    {
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Connect", GUILayout.MaxWidth(100)))
            {
                if (!NetManager.Instance.Connect("127.0.0.1"))
                {
                    Debug.LogError("connecting failed.");
                    return;
                }

            }
            if (GUILayout.Button("Disconnect", GUILayout.MaxWidth(100)))
            {
                NetManager.Instance.Disconnect();
            }
            GUILayout.EndHorizontal();
        }
    }
}
