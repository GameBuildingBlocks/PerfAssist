using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;


public class PASharedWindow : EditorWindow
{
    [MenuItem("Window/PAShared Test")]
    static void Create()
    {
        PASharedWindow w = EditorWindow.GetWindow<PASharedWindow>();
        w.minSize = new Vector2(1280, 720);
        w.Show();
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test_PAChartDataSet", GUILayout.Height(20)))
        {
            Test_PAChartDataSet();
        }
        GUILayout.EndHorizontal();
    }

    void Test_PAChartDataSet()
    {
        PAChartDataSet ds = new PAChartDataSet();
        for (int i = 0; i < 100; i++)
        {
            ds.Append(new PAChartPlotPoint(i, Random.value * 100.0f));
        }

        ds.Shrink(60);

        foreach (var item in ds)
        {
            PAChartPlotPoint pt = item as PAChartPlotPoint;
            if (pt != null)
                Debug.LogFormat("{0}, {1}", pt.m_frameID, pt.m_value);
        }
    }
}
