using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SysUtil
{
    public static string FormatDateAsFileNameString(DateTime dt)
    {
        return string.Format("{0:0000}-{1:00}-{2:00}", dt.Year, dt.Month, dt.Day);
    }

    public static string FormatTimeAsFileNameString(DateTime dt)
    {
        return string.Format("{0:00}-{1:00}-{2:00}", dt.Hour, dt.Minute, dt.Second);
    }
}

public delegate List<UnityEngine.Object> AdditionalMemObjectExtractor(GameObject go);

public class SceneGraphExtractor
{
    public UnityEngine.Object m_root;
    public List<int> GameObjectIDs = new List<int>();

    public static List<string> MemCategories = new List<string>() { "Texture2D", "AnimationClip", "Mesh", "Font", "ParticleSystem", "Camera" };
    public static AdditionalMemObjectExtractor UIWidgetExtractor;

    public Dictionary<string, List<int>> MemObjectIDs = new Dictionary<string, List<int>>();

    void CountMemObject(UnityEngine.Object obj)
    {
        List<int> ids = null;
        if (obj != null && MemObjectIDs.TryGetValue(obj.GetType().Name, out ids))
        {
            if (ids != null && !ids.Contains(obj.GetInstanceID()))
                ids.Add(obj.GetInstanceID());
        }
    }

    void ExtractComponentIDs<T>(GameObject go) where T : Component
    {
        Component[] cameras = go.GetComponentsInChildren(typeof(T), true);
        foreach (T comp in cameras)
        {
            CountMemObject(comp);
        }
    }

    public SceneGraphExtractor(UnityEngine.Object root)
    {
        m_root = root;

        foreach (var item in MemCategories)
            MemObjectIDs[item] = new List<int>();

        var go = m_root as GameObject;
        if (go != null)
        {
            ProcessRecursively(go);

            ExtractComponentIDs<Camera>(go);

#if UNITY_EDITOR
            Component[] renderers = go.GetComponentsInChildren(typeof(Renderer), true);
            foreach (Renderer renderer in renderers)
            {
                foreach (UnityEngine.Object obj2 in EditorUtility.CollectDependencies(new UnityEngine.Object[] { renderer }))
                {
                    List<int> ids = null;
                    if (obj2 != null && MemObjectIDs.TryGetValue(obj2.GetType().Name, out ids))
                    {
                        if (ids != null && !ids.Contains(obj2.GetInstanceID()))
                            ids.Add(obj2.GetInstanceID());
                    }
                }
            }
#else
            if (UIWidgetExtractor != null)
            {
                List<UnityEngine.Object> objs = UIWidgetExtractor(go);
                foreach (var obj in objs)
                {
                    CountMemObject(obj);
                }
            }

            foreach (MeshFilter meshFilter in go.GetComponentsInChildren(typeof(MeshFilter), true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                CountMemObject(mesh);
            }

            foreach (Renderer renderer in go.GetComponentsInChildren(typeof(Renderer), true))
            {
                if(renderer.sharedMaterial!=null)
                {
                    CountMemObject(renderer.sharedMaterial);

                    var txtures = ResourceTracker.Instance.GetTexture2DObjsFromMaterial(renderer.sharedMaterial);
                    foreach (var txture in txtures)
                    {
                        CountMemObject(txture);
                    }
                }
            }
            ExtractComponentIDs<Animator>(go);
            ExtractComponentIDs<ParticleSystem>(go);
#endif
        }
    }

    public void ProcessRecursively(GameObject obj)
    {
        if (!GameObjectIDs.Contains(obj.GetInstanceID()))
            GameObjectIDs.Add(obj.GetInstanceID());

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var child = obj.transform.GetChild(i).gameObject;
            if (child != null)
            {
                ProcessRecursively(child);
            }
        }
    }
}

