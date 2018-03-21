using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public enum TextureCategory
{
    Uncategorized,
    Scene,
    //Character,
    UI,
    All,
    Num,
}

public enum TextureCollectionView
{
    TileView,
    ListView,
}

public class TextureCategorizing
{
    static Dictionary<TextureCategory, GameObject> s_cachedRootObjects = new Dictionary<TextureCategory, GameObject>();

    public static void RefreshRootObjects()
    {
        s_cachedRootObjects.Clear();
        s_cachedRootObjects[TextureCategory.Scene] = GameObject.Find("Scene");
        s_cachedRootObjects[TextureCategory.UI] = GameObject.Find("UI Root(Clone)");

        //if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == SceneDefine.LoginScenePath)
        //{
        //    s_cachedRootObjects[TextureCategory.Character] = GameObject.Find("xrjm_logic");
        //}
        //else
        //{
        //    s_cachedRootObjects[TextureCategory.Character] = GameObject.Find(LogicEditorRoot.RootNodeName);
        //}
    }

    public static int GetTextureCategory(GameObject ownerObj)
    {
        GameObject root = ownerObj.transform.root.gameObject;
        foreach (var p in s_cachedRootObjects)
        {
            if (p.Value == root)
            {
                return (int)p.Key;
            }
        }
        return (int)TextureCategory.Uncategorized;
    }
}

public class TextureInfoItem
{
    public string TextureName = "<texture>";
    public int TextureWidth = 0;
    public int TextureHeight = 0;
    public int TextureSize = 0;
    public int TextureCat = 0;
    public Texture TextureObject;
}

public class TextureCategoryItem
{
    public string TextureCatName = "<texture_cat>";
    public int TextureCat = 0;
    public int TextureCatSize = 0;
    public int TextureCatCount = 0;
}

public class TextureInfoViewer : EditorWindow
{
    [MenuItem(PAEditorConst.MenuPath + "/TextureInfoViewer")]
    static void Create()
    {
        //// Get existing open window or if none, make a new one:
        TextureInfoViewer w = (TextureInfoViewer)EditorWindow.GetWindow(typeof(TextureInfoViewer));
        w.minSize = new Vector2(800, 600);
        w.Show();
    }

    public TextureInfoViewer()
    {
        _textureInfoTable = new TableView(this, typeof(TextureInfoItem));
        _textureInfoTable.AddColumn("TextureName", "Texture Name", 0.5f, TextAnchor.MiddleLeft);
        _textureInfoTable.AddColumn("TextureWidth", "Width", 0.1f, TextAnchor.MiddleCenter);
        _textureInfoTable.AddColumn("TextureHeight", "Height", 0.1f, TextAnchor.MiddleCenter);
        _textureInfoTable.AddColumn("TextureSize", "Size", 0.2f, TextAnchor.MiddleCenter, PAEditorConst.BytesFormatter);
        _textureInfoTable.OnSelected += OnTextureSelected;

        _textureInfoTiles = new TileView(this);
        _textureInfoTiles.OnSelected += OnTextureSelected;

        _textureCatTable = new TableView(this, typeof(TextureCategoryItem));
        _textureCatTable.AddColumn("TextureCatName", "Category Name", 0.5f, TextAnchor.MiddleLeft);
        _textureCatTable.AddColumn("TextureCat", "CatID", 0.1f);
        _textureCatTable.AddColumn("TextureCatSize", "Size", 0.2f, TextAnchor.MiddleCenter, PAEditorConst.BytesFormatter);
        _textureCatTable.AddColumn("TextureCatCount", "Count", 0.2f);
        _textureCatTable.OnSelected += TableView_TextureCategorySelected;
    }

    float _lastStatsTime = 0.0f;
    private void Update()
    {
        if (Application.isPlaying)
        {
            if (_realtimeRefreshing && Time.time - _lastStatsTime > 1.0f)
            {
                Debug.LogFormat("Time: {0}", Time.time);
                _lastStatsTime = Time.time;

                try
                {
                    RefreshTextureInfoTables();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
        else
        {
            if (_lastStatsTime > 0.0f)
            {
                _lastStatsTime = 0.0f;
            }
        }
    }

    private void RefreshTextureInfoTables()
    {
        _visibleMaterials.Clear();
        _visibleTextures.Clear();
        _visibleGameObjects.Clear();

        TextureCategorizing.RefreshRootObjects();

        Renderer[] meshRenderers = UnityEngine.Object.FindObjectsOfType(typeof(Renderer)) as Renderer[];
        foreach (Renderer mr in meshRenderers)
        {
            if (mr.isVisible)
            {
                GameObject go = mr.gameObject;
                //if (_meshLut.AddMesh(go))
                {
                    _nameLut[go.GetInstanceID()] = go.name;

                    //Debug.Log(string.Format("CollectFrameData(): adding game object. {0}, name {1}, name count {2}",
                    //                        go.GetInstanceID(),
                    //                        go.name,
                    //                        _nameLut.Count));

                    foreach (var mat in mr.sharedMaterials)
                    {
                        AddVisibleMaterial(mat, mr.gameObject);

                        if (mat != null)
                        {
#if UNITY_EDITOR
                            if (Application.isEditor)
                            {
                                int cnt = ShaderUtil.GetPropertyCount(mat.shader);
                                for (int i = 0; i < cnt; i++)
                                {
                                    if (ShaderUtil.GetPropertyType(mat.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                                    {
                                        string propName = ShaderUtil.GetPropertyName(mat.shader, i);
                                        AddVisibleTexture(mat.GetTexture(propName), mat, go);
                                    }
                                }
                            }
                            else
#endif
                            {
                                AddVisibleTexture(mat.mainTexture, mat, go);
                            }
                        }
                    }
                }
            }
        }


#if JX3M
        HashSet<Texture> textures = new HashSet<Texture>();
        UISprite[] sprites = UnityEngine.Object.FindObjectsOfType(typeof(UISprite)) as UISprite[];
        foreach (UISprite s in sprites)
        {
            if (!textures.Contains(s.mainTexture))
            {
                AddVisibleTexture(s.mainTexture, s.material, s.gameObject);
            }
        }
#endif

        //UIAtlas[] atlases = UnityEngine.Object.FindObjectsOfType(typeof(UIAtlas)) as UIAtlas[];
        //foreach (UIAtlas a in atlases)
        //{
        //    AddVisibleTexture(a.texture, a.spriteMaterial, a.gameObject);
        //}

        Debug.Log(string.Format("{0} visible materials ({1}), visible textures ({2})",
                          DateTime.Now.ToLongTimeString(),
                          VisibleMaterials.Count,
                          VisibleTextures.Count));

        Dictionary<int, int> countDict = new Dictionary<int, int>();
        Dictionary<int, int> sizeDict = new Dictionary<int, int>();

        countDict[(int)TextureCategory.All] = 0;
        sizeDict[(int)TextureCategory.All] = 0;

        List<object> entries = new List<object>();
        foreach (var p in VisibleTextures)
        {
            TextureInfoItem si = new TextureInfoItem();
            si.TextureName = p.Key.name;
            si.TextureWidth = p.Key.width;
            si.TextureHeight = p.Key.height;
            si.TextureSize = _textureSizeLut[p.Key];
            si.TextureCat = _textureCatLut[p.Key];
            si.TextureObject = p.Key;

            if (_selectedCategory == (TextureCategory)si.TextureCat || _selectedCategory == TextureCategory.All)
            {
                entries.Add(si);
            }

            if (!sizeDict.ContainsKey(si.TextureCat))
            {
                sizeDict.Add(si.TextureCat, 0);
            }
            sizeDict[si.TextureCat] += si.TextureSize;
            sizeDict[(int)TextureCategory.All] += si.TextureSize;

            if (!countDict.ContainsKey(si.TextureCat))
            {
                countDict.Add(si.TextureCat, 0);
            }
            countDict[si.TextureCat] += 1;
            countDict[(int)TextureCategory.All] += 1;
        }
        _textureInfoTable.RefreshData(entries);
        _textureInfoTiles.RefreshData(entries);

        _totalVisibleTextureCount = countDict[(int)TextureCategory.All];
        _totalVisibleTextureSize = sizeDict[(int)TextureCategory.All];

        List<object> catEntries = new List<object>();
        for (int i = 0; i < (int)TextureCategory.Num; ++i)
        {
            TextureCategoryItem cat = new TextureCategoryItem();
            cat.TextureCat = i;
            cat.TextureCatName = ((TextureCategory)i).ToString();
            cat.TextureCatSize = sizeDict.ContainsKey(i) ? sizeDict[i] : 0;
            cat.TextureCatCount = countDict.ContainsKey(i) ? countDict[i] : 0;
            catEntries.Add(cat);
        }
        _textureCatTable.RefreshData(catEntries);

        Repaint();
    }

    private void AddVisibleMaterial(Material mat, GameObject gameobject)
    {
        if (mat != null)
        {
            if (!_visibleMaterials.ContainsKey(mat))
            {
                _visibleMaterials.Add(mat, new HashSet<GameObject>());
            }
            _visibleMaterials[mat].Add(gameobject);
        }
    }


    private void AddVisibleTexture(Texture texture, Material ownerMat, GameObject ownerObj)
    {
        if (texture != null)
        {
            if (!_visibleTextures.ContainsKey(texture))
            {
                _visibleTextures.Add(texture, new HashSet<Material>());
            }
            _visibleTextures[texture].Add(ownerMat);

            if (!_visibleGameObjects.ContainsKey(texture))
            {
                _visibleGameObjects.Add(texture, new List<GameObject>());
            }
            _visibleGameObjects[texture].Add(ownerObj);

            // refresh the size
            if (!_textureSizeLut.ContainsKey(texture))
            {
                //_textureSizeLut[texture] = UsTextureUtil.CalculateTextureSizeBytes(texture);
                _textureSizeLut[texture] =
#if JX3M
                    KProfiler.GetRuntimeMemorySize(texture);
#else
                    (int)Profiler.GetRuntimeMemorySizeLong(texture);
#endif
            }

            // refresh the category
            if (!_textureCatLut.ContainsKey(texture))
            {
                _textureCatLut[texture] = TextureCategorizing.GetTextureCategory(ownerObj);
            }
        }
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

    int _totalVisibleTextureCount = 0;
    int _totalVisibleTextureSize = 0;

    Texture _selectedTexture;
    TextureCollectionView _textureView = 0;

    TextureCategory _selectedCategory = TextureCategory.All;

    void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(MemStyles.Toolbar);
        _realtimeRefreshing = GUILayout.Toggle(_realtimeRefreshing, "Realtime Refreshing", MemStyles.ToolbarButton);
        GUILayout.Space(10);
        _highlightSelection = GUILayout.Toggle(_highlightSelection, "Highlight Selection", MemStyles.ToolbarButton);
        GUILayout.Space(10);
        GUILayout.Label(string.Format("Count: {0}, Size: {1}", _totalVisibleTextureCount, EditorUtility.FormatBytes(_totalVisibleTextureSize)), MemStyles.ToolbarButton);
        GUILayout.Space(10);

        string[] literals = { "Tile View", "List View" };
        int texView = GUILayout.SelectionGrid((int)_textureView, literals, literals.Length, MemStyles.ToolbarButton);
        if (texView != (int)_textureView)
        {
            _textureView = (TextureCollectionView)texView;
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        float toolbar = 30.0f;
        float padding = 10.0f;

        float leftWidth = Mathf.Floor(position.width * 0.7f);
        float rightWidth = position.width - leftWidth - padding;
        float categoryHeight = Mathf.Floor((position.height - toolbar) * 0.3f);

        Rect leftViewRect = new Rect(0, toolbar + padding, leftWidth,
            position.height - padding * 2.0f - toolbar);

        switch (_textureView)
        {
        case TextureCollectionView.TileView:
            GUILayout.BeginArea(leftViewRect);
            _textureInfoTiles.Draw(new Rect(0, 0, leftViewRect.width, leftViewRect.height));
            GUILayout.EndArea();
            break;
        case TextureCollectionView.ListView:
            DrawTable(_textureInfoTable, leftViewRect);
            break;
        default:
            break;
        }

        DrawTable(_textureCatTable, new Rect(leftWidth + padding, toolbar + padding, rightWidth, categoryHeight));

        float consumedHeight = toolbar + padding + categoryHeight + padding;
        Rect r = new Rect(leftWidth + padding, consumedHeight, rightWidth, position.height - consumedHeight);
        GUILayout.BeginArea(r);
        GUILayout.BeginVertical();
        if (_selectedTexture != null)
        {
            GUI.Label(new Rect(0, 0, r.width, 25), string.Format("{0} (w: {1} h: {2})", _selectedTexture.name, _selectedTexture.width, _selectedTexture.height));
            GUI.DrawTexture(new Rect(0, 25, _selectedTexture.width / 2.0f, _selectedTexture.height / 2.0f), _selectedTexture);
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUILayout.EndVertical();
    }

    Dictionary<Material, Texture> _highlighted = new Dictionary<Material, Texture>();


    void GotoHighlightedGameObject(GameObject go)
    {
        try
        {
#if UNITY_EDITOR
            EditorGUIUtility.PingObject(go);
            var currentlActive = Selection.activeGameObject;
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView.FrameSelected();
            Selection.activeGameObject = currentlActive;
#endif
        }
        catch (Exception ex)
        {
            Log.Exception(ex);
            throw;
        }
    }

    void OnTextureSelected(object selected, int col)
    {
        TextureInfoItem item = selected as TextureInfoItem;
        if (item != null)
        {
            _selectedTexture = item.TextureObject;

            if (_highlightSelection)
            {
                foreach (var p in _highlighted)
                    p.Key.mainTexture = p.Value;

                _highlighted.Clear();
                HashSet<Material> mats;
                if (VisibleTextures.TryGetValue(_selectedTexture, out mats))
                {
                    foreach (var mat in mats)
                    {
                        _highlighted[mat] = mat.mainTexture;
                        mat.mainTexture = null;
                    }
                }

                List<GameObject> objects;
                if (_visibleGameObjects.TryGetValue(_selectedTexture, out objects))
                {
                    if (objects != null && objects.Count > 0)
                    {
                        GotoHighlightedGameObject(objects[0]);
                    }
                }
            }
        }
    }

    void TableView_TextureCategorySelected(object selected, int col)
    {
        TextureCategoryItem item = selected as TextureCategoryItem;
        if (item != null)
        {
            _selectedCategory = (TextureCategory)item.TextureCat;

            RefreshTextureInfoTables();
        }
    }

    void OnDestroy()
    {
        if (_textureInfoTable != null)
            _textureInfoTable.Dispose();
        if (_textureInfoTiles != null)
            _textureInfoTiles.Dispose();
        if (_textureCatTable != null)
            _textureCatTable.Dispose();
        if (_matList != null)
            _matList.Dispose();

        _textureInfoTable = null;
        _textureInfoTiles = null;
        _textureCatTable = null;
        _matList = null;
    }

    TableView _textureInfoTable;
    TileView _textureInfoTiles;
    TableView _textureCatTable;
    TableView _matList;

    #region Gathered Meshes/Materials/Textures

    //public MeshLut MeshTable { get { return _meshLut; } }
    //private MeshLut _meshLut = new MeshLut();

    public Dictionary<Material, HashSet<GameObject>> VisibleMaterials { get { return _visibleMaterials; } }
    private Dictionary<Material, HashSet<GameObject>> _visibleMaterials = new Dictionary<Material, HashSet<GameObject>>();

    public Dictionary<Texture, HashSet<Material>> VisibleTextures { get { return _visibleTextures; } }
    private Dictionary<Texture, HashSet<Material>> _visibleTextures = new Dictionary<Texture, HashSet<Material>>();

    private Dictionary<Texture, List<GameObject>> _visibleGameObjects = new Dictionary<Texture, List<GameObject>>();

    private Dictionary<int, string> _nameLut = new Dictionary<int, string>();
    private Dictionary<Texture, int> _textureSizeLut = new Dictionary<Texture, int>();
    private Dictionary<Texture, int> _textureCatLut = new Dictionary<Texture, int>();

    #endregion Gathered Meshes/Materials/Textures

    private bool _realtimeRefreshing = true;
    private bool _highlightSelection = true;
}
