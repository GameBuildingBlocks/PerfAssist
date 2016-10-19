using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PAChartPlotPoint
{
    public PAChartPlotPoint(int frame, float val)
    {
        m_frameID = frame;
        m_value = val;
    }

    public int m_frameID;
    public float m_value;
}

public class PAChartDataSet : IEnumerable
{
    public void Append(PAChartPlotPoint pt)
    {
        m_dataSet.Enqueue(pt);

        if (m_autoShrinkMaxSize > 0 && 
            m_autoShrinkMaxSize < m_dataSet.Count)
        {
            Shrink(m_autoShrinkMaxSize);
        }
    }

    public void Shrink(int newSize)
    {
        while (newSize < m_dataSet.Count)
        {
            m_dataSet.Dequeue();
        }
    }

    public int Count { get { return m_dataSet.Count; } }

    public float MaxValue
    {
        get 
        {
            if (m_dataSet.Count == 0)
                return 0.0f;

            float maxVal = 0.0f;
            foreach (var item in m_dataSet)
                maxVal = Mathf.Max(maxVal, item.m_value);
            return maxVal;
        }
    }

    public int AutoShrinkMaxSize
    {
        get { return m_autoShrinkMaxSize; }
        set { m_autoShrinkMaxSize = value; }
    }

    public IEnumerator GetEnumerator()
    {
        return m_dataSet.GetEnumerator();
    }

    private int m_autoShrinkMaxSize = -1;
    private Queue<PAChartPlotPoint> m_dataSet = new Queue<PAChartPlotPoint>();
}
