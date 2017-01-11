using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
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

public class SceneGraphExtractor
{
    public UnityEngine.Object m_root;
    public List<int> GameObjectIDs = new List<int>();
    public List<int> TextureIDs = new List<int>();
    public List<int> AnimationClipIDs = new List<int>();

    public static List<string> MemCategories = new List<string>() { "Texture2D", "AnimationClip", "Mesh", "Font", "ParticleSystem", "Camera" };

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

#if !UNITY_EDITOR
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
            //foreach (UIWidget w in go.GetComponentsInChildren(typeof(UIWidget), true))
            //{
            //    Material mat = w.material;
            //    if (mat != null)
            //    {
            //        CountMemObject(mat);
            //    }
            //    Texture2D t = w.mainTexture as Texture2D;
            //    if (t != null)
            //    {
            //        CountMemObject(t);
            //    }
            //}

            var shaderPropertyDict =ResourceTracker.Instance.ShaderPropertyDict;
            foreach (MeshFilter meshFilter in go.GetComponentsInChildren(typeof(MeshFilter), true))
            {
                Mesh mesh = meshFilter.sharedMesh;
                CountMemObject(mesh);
            }

            foreach (Renderer renderer in go.GetComponentsInChildren(typeof(Renderer), true))
            {
                Material mat = renderer.sharedMaterial;
                if (mat != null)
                {
                    CountMemObject(mat);

                    Shader shader = mat.shader;
                    if (shader != null && shaderPropertyDict != null && shaderPropertyDict.ContainsKey(shader.name))
                    {
                        string propertyNameStrs;
                        shaderPropertyDict.TryGetValue(shader.name, out propertyNameStrs);
                        char[] tokens = new char[] { ResourceTrackerConst.shaderPropertyNameJsonToken };
                        var propertyNameList = propertyNameStrs.Split(tokens);
                        foreach (var propertyName in propertyNameList)
                        {
                            Texture2D tex = mat.GetTexture(propertyName) as Texture2D;
                            if (tex != null)
                            {
                                CountMemObject(tex);
                            }
                        }
                    }
                    else
                    {
                        if (mat.mainTexture is Texture2D)
                        {
                            CountMemObject(mat.mainTexture);
                        }
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

