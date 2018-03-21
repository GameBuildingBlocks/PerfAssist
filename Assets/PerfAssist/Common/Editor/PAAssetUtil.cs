using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PAAssetUtil
{
    // http://answers.unity3d.com/questions/486545/getting-all-assets-of-the-specified-type.html
    public static List<T> FindByType<T>() where T : UnityEngine.Object
    {
        bool showProgressBar = false;

        string t;
        if (typeof(T) == typeof(GameObject))
        {
            t = "t:Prefab";
            showProgressBar = true;
        }
        else
        {
            t = string.Format("t:{0}", typeof(T).ToString().Replace("UnityEngine.", ""));
        }

        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(t);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (showProgressBar)
                EditorUtility.DisplayCancelableProgressBar("FindByType",
                    string.Format("Loading {0} ({1}/{2})", assetPath, i, guids.Length),
                    (float)i / (float)guids.Length);

            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                assets.Add(asset);
            }
        }

        if (showProgressBar)
            EditorUtility.ClearProgressBar();
        return assets;
    }
}
