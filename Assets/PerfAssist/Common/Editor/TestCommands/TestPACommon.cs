using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class TestPACommon : EditorWindow
{
    [MenuItem(PAEditorConst.DemoTestPath + "/Test UniqueString")]
    static void TestUniqueString()
    {
        int i = 15;
        string s = "foo" + i.ToString();
        string s2 = UniqueString.Intern(s);
        string s3 = UniqueString.Intern(s, false);
        Debug.Log(System.Object.ReferenceEquals(s, s2));
        Debug.Log(System.Object.ReferenceEquals(s, s3));
        UniqueString.Clear();
    }

    [MenuItem(PAEditorConst.DemoTestPath + "/Test ChartDataSet")]
    static void TestPAChartDataSet()
    {
        PAChartDataSet ds = new PAChartDataSet(100);
        for (int i = 0; i < 100; i++)
        {
            ds.Append(new PAChartPlotPoint(i, Random.value * 100.0f));
        }

        Assert.IsTrue(ds.Count == 100);

        ds.ShrinkTo(60);

        Assert.IsTrue(ds.Count == 60);
    }
}
