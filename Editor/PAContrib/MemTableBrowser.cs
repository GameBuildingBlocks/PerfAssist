using MemoryProfilerWindow;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public struct sDiffType
{
    public static readonly string AdditiveType = "(added)";
    public static readonly string NegativeType = "(removed)";
    public static readonly string ModificationType = "(modified)";
}

public class MemCategory
{
    public int Category;
    public int Count;
    public int Size;
}

public class MemType
{
    public string TypeName = "Foo";
    public int Count = 0;
    public int Size = 0;
    public string SizeLiterally = "";

    public List<object> Objects = new List<object>();

    public int Category = 0;

    public void AddObject(MemObject mo)
    {
        Objects.Add(mo);
        Count = Objects.Count;
        Size += mo.Size;
        SizeLiterally = EditorUtility.FormatBytes(Size);
    }
}

public class MemObject
{
    public string InstanceName;
    public int Size = 0;
    public int RefCount = 0;

    public MemObject(ThingInMemory thing, CrawledMemorySnapshot unpacked)
    {
        _thing = thing;

        if (_thing != null)
        {
            var mo = thing as ManagedObject;

            if (mo != null && mo.typeDescription.name == "System.String")
            {
                try
                {
                    InstanceName = StringTools.ReadString(unpacked.managedHeap.Find(mo.address, unpacked.virtualMachineInformation), unpacked.virtualMachineInformation);
                }
                catch (System.Exception ex)
                {
                    //UnityEngine.Debug.LogErrorFormat("StringTools.ReadString happens error .things caption = {0} ,ex ={1} ", thing.caption, ex.ToString());
                    var bo =unpacked.managedHeap.Find(mo.address, unpacked.virtualMachineInformation);
                    if (bo.bytes == null)
                    {
                        InstanceName = string.Format("error string,find address bytes is null ,caption = {0},address = {1},exception ={2}", thing.caption,mo.address, ex.ToString());
                        UnityEngine.Debug.LogErrorFormat("error string,find address bytes is null ,caption = {0},address = {1},exception ={2}", thing.caption, mo.address, ex.ToString());
                    }
                    else {
                        var lengthPointer = bo.Add(unpacked.virtualMachineInformation.objectHeaderSize);
                        var length = lengthPointer.ReadInt32();
                        var firstChar = lengthPointer.Add(4);
                        InstanceName = string.Format("error string,expect caption = {0} ,length = {1},firstChar ={2},address = {3},exception ={4}", thing.caption, length, firstChar, mo.address, ex.ToString());
                        UnityEngine.Debug.LogErrorFormat("error string,expect caption = {0} ,length = {1},firstChar ={2},address = {3},exception ={4}", thing.caption, length, firstChar, mo.address, ex.ToString());
                    }
                }
            }
            else
            {
                InstanceName = _thing.caption;
            }

            Size = _thing.size;
            RefCount = _thing.referencedBy.Length;
        }
    }

    public ThingInMemory _thing;
}

public class MemTableBrowser
{
    CrawledMemorySnapshot _unpacked;
    CrawledMemorySnapshot _preUnpacked;

    TableView _typeTable;
    TableView _objectTable;
    EditorWindow _hostWindow;
    
    Dictionary<string, MemType> _types = new Dictionary<string, MemType>();
    
    private string[] _categoryLiterals = new string[MemConst.MemTypeCategories.Length];

    private int _memTypeCategory = 0;
    private int _memTypeSizeLimiter = 0;

    string _searchInstanceString = "";
    string _searchTypeString = "";

    public MemTableBrowser(EditorWindow hostWindow)
    {
        _hostWindow = hostWindow;

        // create the table with a specified object type
        _typeTable = new TableView(hostWindow, typeof(MemType));
        _objectTable = new TableView(hostWindow, typeof(MemObject));

        // setup the description for content
        _typeTable.AddColumn("TypeName", "Type Name", 0.6f, TextAnchor.MiddleLeft);
        _typeTable.AddColumn("Count", "Count", 0.15f);
        _typeTable.AddColumn("Size", "Size", 0.25f, TextAnchor.MiddleCenter, PAEditorConst.BytesFormatter);

        _objectTable.AddColumn("InstanceName", "Instance Name", 0.8f, TextAnchor.MiddleLeft);
        _objectTable.AddColumn("Size", "Size", 0.1f, TextAnchor.MiddleCenter, PAEditorConst.BytesFormatter);
        _objectTable.AddColumn("RefCount", "Refs", 0.1f);

        // sorting
        _typeTable.SetSortParams(2, true);
        _objectTable.SetSortParams(1, true);

        // register the event-handling function
        _typeTable.OnSelected += OnTypeSelected;
        _objectTable.OnSelected += OnObjectSelected;
    }

    public void ClearTable()
    {
        _types.Clear();
        _typeTable.RefreshData(null);
        _objectTable.RefreshData(null);
    }

    public void ShowSingleSnapshot(CrawledMemorySnapshot unpacked)
    {
        ClearTable();
        _unpacked = unpacked;
        if (_unpacked == null)
            return;

        _types = SnapshotUtil.PopulateTypes(_unpacked);

        var categories = SnapshotUtil.PopulateCategories(_unpacked);
        _categoryLiterals = SnapshotUtil.FormulateCategoryLiterals(categories);

        RefreshTables();
    }

    public void ShowDiffedSnapshots(CrawledMemorySnapshot diff1st, CrawledMemorySnapshot diff2nd)
    {
        ClearTable();
        _unpacked = diff2nd;
        if (_unpacked == null)
            return;

        var types1st = SnapshotUtil.PopulateTypes(diff1st);
        var types2nd = SnapshotUtil.PopulateTypes(diff2nd);
        _types = SnapshotUtil.DiffTypes(types1st, types2nd);

        var categories1st = SnapshotUtil.PopulateCategories(diff1st);
        var categories2nd = SnapshotUtil.PopulateCategories(diff2nd);
        _categoryLiterals = SnapshotUtil.FormulateCategoryLiteralsDiffed(categories1st, categories2nd);

        RefreshTables();
    }

    private Dictionary<object, Color> getSpecialColorDict(List<object> objs){
        Dictionary<object, Color> resultDict=  new Dictionary<object, Color>();
        foreach (object obj in objs)
        {
            var memType = obj as MemType;

            string typeName = memType.TypeName;
            if (typeName.Length == 0)
                continue;

            if (typeName.Contains(sDiffType.AdditiveType))
            {
                resultDict.Add(obj, Color.green);
            }
            else
                if (typeName.Contains(sDiffType.NegativeType))
                {
                    resultDict.Add(obj, Color.red);
                }
                else
                    if (typeName.Contains(sDiffType.ModificationType))
                    {
                        resultDict.Add(obj, Color.blue);
                    }
        }
        return resultDict;
    }

    public void RefreshTables()
    {
        if (_unpacked == null)
        {
            _typeTable.RefreshData(null);
            _objectTable.RefreshData(null);
            return;
        }

        MemType searchResultType = null;
        List<object> qualified = new List<object>();
        if (!string.IsNullOrEmpty(_searchInstanceString))
        {
            // search for instances
            _types.Remove(MemConst.SearchResultTypeString);
            searchResultType = new MemType();
            searchResultType.TypeName = MemConst.SearchResultTypeString + " " + _searchInstanceString;
            searchResultType.Category = 0;
            searchResultType.Objects = new List<object>();

            string search = _searchInstanceString.ToLower();
            foreach (ThingInMemory thingInMemory in _unpacked.allObjects)
            {
                if (thingInMemory.caption.ToLower().Contains(search))
                {
                    searchResultType.AddObject(new MemObject(thingInMemory, _unpacked));
                }
            }

            qualified.Add(searchResultType);
            _types.Add(MemConst.SearchResultTypeString, searchResultType);
        }
        else
        {
            // ordinary case - list categorized types and instances
            foreach (var p in _types)
            {
                MemType mt = p.Value;

                bool isAll = _memTypeCategory == 0;
                bool isNative = _memTypeCategory == 1 && mt.Category == 1;
                bool isManaged = _memTypeCategory == 2 && mt.Category == 2;
                bool isOthers = _memTypeCategory == 3 && (mt.Category == 3 || mt.Category == 4);
                if (isAll || isNative || isManaged || isOthers)
                {
                    if (MemUtil.MatchSizeLimit(mt.Size, _memTypeSizeLimiter))
                    {
                        if (string.IsNullOrEmpty(_searchTypeString) || p.Key.ToLower().Contains(_searchTypeString.ToLower()))
                            qualified.Add(mt);
                    }
                }
            }
        }

        _typeTable.RefreshData(qualified, getSpecialColorDict(qualified));
        _objectTable.RefreshData(null);

        if (searchResultType != null)
            _typeTable.SetSelected(searchResultType);
    }

    public void Draw(Rect r)
    {
        int border = MemConst.TableBorder;
        float split = MemConst.SplitterRatio;
        int toolbarHeight = 50;

        GUILayout.BeginArea(r, MemStyles.Background);
        GUILayout.BeginHorizontal(MemStyles.Toolbar);
        // categories
        {
            GUILayout.Label("Category: ", GUILayout.MinWidth(120));

            string[] literals = _unpacked != null ? _categoryLiterals : MemConst.MemTypeCategories;

            int newCategory = GUILayout.SelectionGrid(_memTypeCategory, literals, literals.Length, MemStyles.ToolbarButton);
            if (newCategory != _memTypeCategory)
            {
                _memTypeCategory = newCategory;
                RefreshTables();
            }
        }
        GUILayout.FlexibleSpace();

        // size limiter
        {
            GUILayout.Label("Size: ", GUILayout.MinWidth(120));
            int newLimiter = GUILayout.SelectionGrid(_memTypeSizeLimiter, MemConst.MemTypeLimitations, MemConst.MemTypeLimitations.Length, MemStyles.ToolbarButton);
            if (newLimiter != _memTypeSizeLimiter)
            {
                _memTypeSizeLimiter = newLimiter;
                RefreshTables();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(3);

        GUILayout.BeginHorizontal(MemStyles.Toolbar);

        // search box - types
        {
            string enteredString = GUILayout.TextField(_searchTypeString, 100, MemStyles.SearchTextField, GUILayout.MinWidth(200));
            if (enteredString != _searchTypeString)
            {
                _searchTypeString = enteredString;
                RefreshTables();
            }
            if (GUILayout.Button("", MemStyles.SearchCancelButton))
            {
                _searchTypeString = "";
                GUI.FocusControl(null); // Remove focus if cleared
                RefreshTables();
            }
        }

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Show Type Stats", EditorStyles.toolbarButton))
        {
            MemStats.ShowTypeStats(_typeTable.GetSelected() as MemType);
        }
        GUILayout.FlexibleSpace();

        // search box - instances
        {
            string enteredString = GUILayout.TextField(_searchInstanceString, 100, MemStyles.SearchTextField, GUILayout.MinWidth(200));
            if (enteredString != _searchInstanceString)
            {
                _searchInstanceString = enteredString;
                RefreshTables();
            }
            if (GUILayout.Button("", MemStyles.SearchCancelButton))
            {
                _types.Remove(MemConst.SearchResultTypeString);
                _searchInstanceString = "";
                GUI.FocusControl(null); // Remove focus if cleared
                RefreshTables();
            }
        }

        GUILayout.EndHorizontal();

        int startY = toolbarHeight + border;
        int height = (int)(r.height - border * 2 - toolbarHeight);
        if (_typeTable != null)
            _typeTable.Draw(new Rect(border, startY, (int)(r.width * split - border * 1.5f), height));
        if (_objectTable != null)
            _objectTable.Draw(new Rect((int)(r.width * split + border * 0.5f), startY, (int)r.width * (1.0f - split) - border * 1.5f, height));
        GUILayout.EndArea();
    }

    void OnTypeSelected(object selected, int col)
    {
        MemType mt = selected as MemType;
        if (mt == null)
            return;

        _objectTable.RefreshData(mt.Objects);
    }

    void OnObjectSelected(object selected, int col)
    {
        var mpw = _hostWindow as MemoryProfilerWindow.MemoryProfilerWindow;
        if (mpw == null)
            return;

        var memObject = selected as MemObject;
        if (memObject == null)
            return;

        mpw.SelectThing(memObject._thing);
    }

    public void SelectThing(ThingInMemory thing)
    {
        if (_searchInstanceString == "")
        {
            string typeName = MemUtil.GetGroupName(thing);

            MemType mt=null;
            if (!_types.TryGetValue(typeName, out mt))
                return;

            if (_typeTable.GetSelected() != mt)
            {
                _typeTable.SetSelected(mt);
                _objectTable.RefreshData(mt.Objects);
            }

            foreach (var item in mt.Objects)
            {
                var mo = item as MemObject;
                if (mo != null && mo._thing == thing)
                {
                    if (_objectTable.GetSelected() != mo)
                    {
                        _objectTable.SetSelected(mo);
                    }
                    break;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (_typeTable != null)
            _typeTable.Dispose();
        if (_objectTable != null)
            _objectTable.Dispose();

        _typeTable = null;
        _objectTable = null;
    }
}
