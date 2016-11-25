using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using MemoryProfilerWindow;
using Assets.Editor.Treemap;

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
                InstanceName = StringTools.ReadString(unpacked.managedHeap.Find(mo.address, unpacked.virtualMachineInformation), unpacked.virtualMachineInformation);
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

    TableView _typeTable;
    TableView _objectTable;
    EditorWindow _hostWindow;

    private Dictionary<string, MemType> _types = new Dictionary<string, MemType>();

    private int _memTypeCategory = 0;
    private int _memTypeSizeLimiter = 0;

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

    public void RefreshData(CrawledMemorySnapshot unpackedCrawl)
    {
        _unpacked = unpackedCrawl;

        _types.Clear();
        foreach (ThingInMemory thingInMemory in _unpacked.allObjects)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;

            MemType theType;
            if (!_types.ContainsKey(typeName))
            {
                theType = new MemType();
                theType.TypeName = MemUtil.GetCategoryLiteral(thingInMemory) + typeName;
                theType.Category = MemUtil.GetCategory(thingInMemory);
                theType.Objects = new List<object>();
                _types.Add(typeName, theType);
            }
            else
            {
                theType = _types[typeName];
            }

            MemObject item = new MemObject(thingInMemory, _unpacked);
            theType.AddObject(item);
        }

        RefreshTables();
    }

    public void RefreshTables()
    {
        if (_unpacked == null)
            return;

        List<object> qualified = new List<object>();
        foreach (var p in _types)
        {
            MemType mt = p.Value;

            // skip this type if category mismatched
            if (_memTypeCategory != 0 && 
                _memTypeCategory != mt.Category)
            {
                continue;
            }

            if (!MemUtil.MatchSizeLimit(mt.Size, _memTypeSizeLimiter))
                continue;

            qualified.Add(mt);
        }

        _typeTable.RefreshData(qualified);
        _objectTable.RefreshData(null);
    }

    public void Draw(Rect r)
    {
        int border = MemConst.TableBorder;
        float split = MemConst.SplitterRatio;

        GUILayout.BeginArea(r, MemStyles.Background);

        int toolbarHeight = 30;
        GUILayout.BeginHorizontal(MemStyles.Toolbar);
        GUILayout.Label("Category: ");
        int newCategory = GUILayout.SelectionGrid(_memTypeCategory, MemConst.MemTypeCategories, MemConst.MemTypeCategories.Length, MemStyles.ToolbarButton);
        if (newCategory != _memTypeCategory)
        {
            _memTypeCategory = newCategory;
            RefreshTables();
        }
        GUILayout.Space(50);
        GUILayout.Label("Size Limit: ");
        int newLimiter = GUILayout.SelectionGrid(_memTypeSizeLimiter, MemConst.MemTypeLimitations, MemConst.MemTypeLimitations.Length, MemStyles.ToolbarButton);
        if (newLimiter != _memTypeSizeLimiter)
        {
            _memTypeSizeLimiter = newLimiter;
            RefreshTables();
        }
        GUILayout.FlexibleSpace();
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
        string typeName = MemUtil.GetGroupName(thing);

        MemType mt;
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
