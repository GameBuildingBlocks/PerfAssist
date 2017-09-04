using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ShaderItem
{
    public string ShaderName = "shader";
    public int ShaderRefCount = 0;
}

public class MaterialItem
{
    public string MatName = "mat_name";
    public string MatPath = "mat_path";
    public Material MatObj;
}

public class PAShaderCleanupWindow : EditorWindow
{
    [MenuItem(PAEditorConst.MenuPath + "/Shader 清理和优化")]
    static void Create()
    {
        PAShaderCleanupWindow w = EditorWindow.GetWindow<PAShaderCleanupWindow>();
        if (w.GetType().Name == "PAShaderCleanupWindow")
        {
            w.minSize = new Vector2(800, 600);
            w.Show();
        }
    }

    void Awake()
    {
        _shaderRefTable = new TableView(this, typeof(ShaderItem));
        _shaderRefTable.AddColumn("ShaderName", "ShaderName", 0.8f, TextAnchor.MiddleLeft);
        _shaderRefTable.AddColumn("ShaderRefCount", "RefCount", 0.2f);
        _shaderRefTable.OnSelected += TableView_ShaderSelected;

        _otherShaderRefTable = new TableView(this, typeof(ShaderItem));
        _otherShaderRefTable.AddColumn("ShaderName", "ShaderName", 0.8f, TextAnchor.MiddleLeft);
        _otherShaderRefTable.AddColumn("ShaderRefCount", "RefCount", 0.2f);
        _otherShaderRefTable.OnSelected += TableView_ShaderSelected;

        _matList = new TableView(this, typeof(MaterialItem));
        _matList.AddColumn("MatName", "Name", 0.3f, TextAnchor.MiddleLeft);
        _matList.AddColumn("MatPath", "Path", 0.7f, TextAnchor.MiddleLeft);
        _matList.OnSelected += TableView_MaterialSelected;

        RefreshTables();
    }

    void RefreshTables()
    {
        Debug.LogFormat("Looking for shaders...");
        List<Shader> shaders = PAAssetUtil.FindByType<Shader>();
        Debug.LogFormat("{0} shaders found.", shaders.Count);

        Debug.LogFormat("Looking for materials...");
        List<Material> materials = PAAssetUtil.FindByType<Material>();
        Debug.LogFormat("{0} materials found.", materials.Count);

        _shaderDict.Clear();
        foreach (var s in shaders)
            _shaderDict[s] = 0;

        Dictionary<Shader, int> otherShaderDict = new Dictionary<Shader, int>();

        foreach (var m in materials)
        {
            if (_shaderDict.ContainsKey(m.shader))
            {
                _shaderDict[m.shader]++;
            }
            else
            {
                if (otherShaderDict.ContainsKey(m.shader))
                {
                    otherShaderDict[m.shader]++;
                }
                else
                {
                    otherShaderDict[m.shader] = 1;
                }
            }

            if (!_otherMaterials.ContainsKey(m.shader.name))
            {
                _otherMaterials.Add(m.shader.name, new List<Material>());
            }
            _otherMaterials[m.shader.name].Add(m);
        }

        List<object> entries = new List<object>();
        foreach (var p in _shaderDict)
        {
            ShaderItem si = new ShaderItem();
            si.ShaderName = p.Key.name;
            si.ShaderRefCount = p.Value;
            entries.Add(si);
        }
        _shaderRefTable.RefreshData(entries);

        List<object> otherEntries = new List<object>();
        foreach (var p in otherShaderDict)
        {
            ShaderItem si = new ShaderItem();
            si.ShaderName = p.Key.name;
            si.ShaderRefCount = p.Value;
            otherEntries.Add(si);
        }
        _otherShaderRefTable.RefreshData(otherEntries);

        _matList.RefreshData(null);
    }

    void DrawTable(TableView table, Rect rect)
    {
        if (table != null)
        {
            GUILayout.BeginArea(rect);
            table.Draw(new Rect(0, 0, rect.width, rect.height));
            GUILayout.EndArea();
        }
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(MemStyles.Toolbar);
        if (GUILayout.Button("Refresh", MemStyles.ToolbarButton))
        {
            RefreshTables();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Find all unused 'jx3Art' shaders", MemStyles.ToolbarButton))
        {
            FindAllUnusedJx3ArtShaders();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        float toolbar = 30.0f;
        float padding = 30.0f;

        float halfWidth = Mathf.Floor(position.width * 0.5f);
        float halfHeight = Mathf.Floor((position.height - toolbar) * 0.5f);

        float tableWidth = halfWidth - padding * 1.5f;
        float tableHeight = halfHeight - padding * 1.5f;

        DrawTable(_shaderRefTable, new Rect(padding, toolbar + padding, tableWidth,
            position.height - padding * 2.0f - toolbar));
        DrawTable(_otherShaderRefTable, new Rect(halfWidth + padding * 0.5f, toolbar + padding,
            tableWidth, tableHeight));
        DrawTable(_matList, new Rect(halfWidth + padding * 0.5f, toolbar + halfHeight + padding * 0.5f,
            tableWidth, tableHeight));

        GUILayout.EndVertical();
    }

    void TableView_ShaderSelected(object selected, int col)
    {
        ShaderItem foo = selected as ShaderItem;
        if (foo == null)
        {
            Debug.LogErrorFormat("the selected object is not a valid one. ({0} expected, {1} got)",
                typeof(ShaderItem).ToString(), selected.GetType().ToString());
            return;
        }

        List<Material> materials = null;
        if (_otherMaterials.TryGetValue(foo.ShaderName, out materials))
        {
            List<object> matEntries = new List<object>();
            foreach (var m in materials)
            {
                MaterialItem si = new MaterialItem();
                si.MatName = m.name;
                si.MatPath = AssetDatabase.GetAssetPath(m);
                si.MatObj = m;
                matEntries.Add(si);
            }
            _matList.RefreshData(matEntries);
        }
        else
        {
            _matList.RefreshData(null);
        }
    }

    void TableView_MaterialSelected(object selected, int col)
    {
        MaterialItem foo = selected as MaterialItem;
        if (foo == null)
        {
            Debug.LogErrorFormat("the selected object is not a valid one. ({0} expected, {1} got)",
                typeof(MaterialItem).ToString(), selected.GetType().ToString());
            return;
        }

        Debug.LogFormat("mat selected. ({0}, {1})", foo.MatName, foo.MatPath);
        EditorGUIUtility.PingObject(foo.MatObj);
    }

    void OnDestroy()
    {
        if (_shaderRefTable != null)
            _shaderRefTable.Dispose();
        if (_otherShaderRefTable != null)
            _otherShaderRefTable.Dispose();
        if (_matList != null)
            _matList.Dispose();

        _shaderRefTable = null;
        _otherShaderRefTable = null;
        _matList = null;
    }

    void FindAllUnusedJx3ArtShaders()
    {
        List<Shader> unused = new List<Shader>();
        HashSet<string> androidSet = new HashSet<string>();
        foreach (var p in _shaderDict)
        {
            if (p.Key.name.StartsWith("jx3Art/") && p.Value == 0)
            {
                unused.Add(p.Key);
            }
            else if (p.Key.name.StartsWith("android/jx3Art/"))
            {
                androidSet.Add(p.Key.name);
            }
        }

        List<Shader> movedSafely = new List<Shader>();
        List<Shader> kept = new List<Shader>();
        foreach (var s in unused)
        {
            if (androidSet.Contains("android/" + s.name))
            {
                movedSafely.Add(s);
            }
            else
            {
                kept.Add(s);
            }
        }

        Debug.LogWarningFormat("===== Kept ===== {0}", kept.Count);
        foreach (var item in kept)
            Debug.Log(AssetDatabase.GetAssetPath(item));

        Debug.LogWarningFormat("===== Moved ===== {0}", movedSafely.Count);
        foreach (var item in movedSafely)
            Debug.Log(AssetDatabase.GetAssetPath(item));
    }

    TableView _shaderRefTable;
    TableView _otherShaderRefTable;
    TableView _matList;

    Dictionary<Shader, int> _shaderDict = new Dictionary<Shader, int>();

    Dictionary<string, List<Material>> _otherMaterials = new Dictionary<string, List<Material>>();
}
