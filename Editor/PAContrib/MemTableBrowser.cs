using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using MemoryProfilerWindow;
using Assets.Editor.Treemap;
using UnityEditorInternal;

using System.IO;
using System.Text;
using PerfAssist.LitJson;
using System.Diagnostics;


public struct sDiffDictKey
{
    public static readonly string addedDict = "added";
    public static readonly string unchangedDict = "unchanged";
    public static readonly string removedDict = "removed";
}

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
    UnPackedInfos _unpackedInfos;

    TableView _typeTable;
    TableView _objectTable;
    EditorWindow _hostWindow;
    
    public bool _showdiffToggle = false;

    Dictionary<string,Dictionary<string,MemType>> _diffDicts = new Dictionary<string,Dictionary<string,MemType>>();
    
    private Dictionary<int, MemCategory> _categories = new Dictionary<int, MemCategory>();
    private string[] _categoryLiterals = new string[MemConst.MemTypeCategories.Length];

    private int _memTypeCategory = 0;
    private int _memTypeSizeLimiter = 0;

    string _searchInstanceString = "";
    string _searchTypeString = "";
    MemType _searchResultType;

    StaticDetailInfo _staticDetailInfo = new StaticDetailInfo();

    private class UnPackedInfos
    {
        CrawledMemorySnapshot unpacked; 
        CrawledMemorySnapshot preUnpacked;


        public Dictionary<int, ThingInMemory> _unpackedThingsDict = new Dictionary<int,ThingInMemory>();
        public Dictionary<int, ThingInMemory> _preunpackedThingsDict = new Dictionary<int, ThingInMemory>();

        public UnPackedInfos(CrawledMemorySnapshot unpacked, CrawledMemorySnapshot preUnpacked)
        {
            this.unpacked = unpacked;
            this.preUnpacked = preUnpacked;
        }

        public void calculateCrawledDict()
        {
            if (preUnpacked == null)
                return;

            foreach (NativeUnityEngineObject nat in unpacked.nativeObjects)
            {
                if (nat == null)
                    continue;
                int key =getNativeObjHashCode(nat);
                if (!_unpackedThingsDict.ContainsKey(key))
                    _unpackedThingsDict.Add(key, nat);
            }

            foreach (NativeUnityEngineObject nat in preUnpacked.nativeObjects)
            {
                if (nat == null)
                    continue;
                int key = getNativeObjHashCode(nat);
                if (!_preunpackedThingsDict.ContainsKey(key))
                    _preunpackedThingsDict.Add(key, nat);
            }

            foreach (ManagedObject mao in unpacked.managedObjects)
            {
                if (mao == null)
                    continue;
                int key = getManagedObjHashCode(mao, unpacked);
                if (key == -1)
                    continue;
                if (!_unpackedThingsDict.ContainsKey(key))
                    _unpackedThingsDict.Add(key, mao);
            }

            foreach (ManagedObject mao in preUnpacked.managedObjects)
            {
                if (mao == null)
                    continue;
                int key = getManagedObjHashCode(mao, preUnpacked);
                if (key == -1)
                    continue;
                if (!_preunpackedThingsDict.ContainsKey(key))
                    _preunpackedThingsDict.Add(key, mao);
            }
        }

        int getNativeObjHashCode(NativeUnityEngineObject nat) 
        {
            return nat.instanceID.GetHashCode();
        }

        int getManagedObjHashCode(ManagedObject mao ,CrawledMemorySnapshot unpacked)
        {
            var ba =unpacked.managedHeap.Find(mao.address, unpacked.virtualMachineInformation);
            var result =getBytesFromHeap(ba,mao.size);
            if (result != null && result.Length>0)
                return result.GetHashCode() * mao.size;
            return -1;
        }

        byte[] getBytesFromHeap(BytesAndOffset ba, int size)
        {
            byte[] result = new byte[size];
            for (int i = 0; i < size;i++)
            {
                result[i] = ba.bytes[i+ba.offset];
            }
            return result;
        }
    }
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

    public void clearTableData() {
        resetDiffDicts();
        _categories.Clear();
        RefreshTables();
    }

    public void resetDiffDicts()
    {
        _diffDicts.Clear();
        _diffDicts.Add(sDiffDictKey.addedDict,new Dictionary<string,MemType>());
        _diffDicts.Add(sDiffDictKey.unchangedDict, new Dictionary<string, MemType>());
        _diffDicts.Add(sDiffDictKey.removedDict, new Dictionary<string, MemType>());
    }

    private void calculateCategoryInfo()
    {
        int[] sizes = new int[MemConst.MemTypeCategories.Length];
        int[] counts = new int[MemConst.MemTypeCategories.Length];
        foreach (var item in _categories)
        {
            sizes[0] += item.Value.Size;
            counts[0] += item.Value.Count;

            if (item.Key == 1)
            {
                sizes[1] += item.Value.Size;
                counts[1] += item.Value.Count;
            }
            else if (item.Key == 2)
            {
                sizes[2] += item.Value.Size;
                counts[2] += item.Value.Count;
            }
            else
            {
                sizes[3] += item.Value.Size;
                counts[3] += item.Value.Count;
            }
        }

        for (int i = 0; i < _categoryLiterals.Length; i++)
        {
            _categoryLiterals[i] = string.Format("{0} ({1}, {2})", MemConst.MemTypeCategories[i], counts[i], EditorUtility.FormatBytes(sizes[i]));
        }
    }

    public void RefreshData(CrawledMemorySnapshot unpackedCrawl)
    {
        _unpacked = unpackedCrawl;
        resetDiffDicts();
        _categories.Clear();
        _staticDetailInfo.clear();
        foreach (ThingInMemory thingInMemory in _unpacked.allObjects)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;

            int category = MemUtil.GetCategory(thingInMemory);

            MemObject item = new MemObject(thingInMemory, _unpacked);
            if (! _staticDetailInfo.isDetailStaticFileds(typeName, thingInMemory.caption,item.Size))
            {
                MemType theType;

                Dictionary<string,MemType> unchangedDict;
                _diffDicts.TryGetValue(sDiffDictKey.unchangedDict,out unchangedDict);
                if (!unchangedDict.ContainsKey(typeName))
                {
                    theType = new MemType();
                    theType.TypeName = MemUtil.GetCategoryLiteral(thingInMemory) + typeName;
                    theType.Category = category;
                    theType.Objects = new List<object>();
                    unchangedDict.Add(typeName, theType);
                }
                else
                {
                    theType = unchangedDict[typeName];
                }
                theType.AddObject(item);
            }

            MemCategory theCategory;
            if (!_categories.TryGetValue(category, out theCategory))
            {
                theCategory = new MemCategory();
                theCategory.Category = category;
                _categories.Add(category, theCategory);
            }
            theCategory.Size += item.Size;
            theCategory.Count++;
        }

        calculateCategoryInfo();
        RefreshTables();
    }

    public void RefreshDiffData(CrawledMemorySnapshot unpackedCrawl, CrawledMemorySnapshot preUnpackedCrawl)
    {
        MemUtil.LoadSnapshotProgress(0.01f, "refresh Data");
        _unpacked = unpackedCrawl;
        _preUnpacked = preUnpackedCrawl;
        resetDiffDicts();
        _categories.Clear();
        _staticDetailInfo.clear();
        MemUtil.LoadSnapshotProgress(0.1f, "refresh data init");
        foreach (ThingInMemory thingInMemory in _unpacked.allObjects)
        {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                continue;
            int category = MemUtil.GetCategory(thingInMemory);
            MemObject item = new MemObject(thingInMemory, _unpacked);
            MemCategory theCategory;
            if (!_categories.TryGetValue(category, out theCategory))
            {
                theCategory = new MemCategory();
                theCategory.Category = category;
                _categories.Add(category, theCategory);
            }
            theCategory.Size += item.Size;
            theCategory.Count++;
        }
        calculateCategoryInfo();

        MemUtil.LoadSnapshotProgress(0.4f, "unpack all objs");
        _unpackedInfos = new UnPackedInfos(_unpacked, _preUnpacked);
        _unpackedInfos.calculateCrawledDict();

        List<ThingInMemory> addedList;
        List<ThingInMemory> unchangedList;
        List<ThingInMemory> removedList;
        getDiffDict(_unpackedInfos._unpackedThingsDict, _unpackedInfos._preunpackedThingsDict, out addedList, out removedList, out unchangedList);
        MemUtil.LoadSnapshotProgress(0.5f, "get diff objs");
        foreach (var thing in addedList)
            _handleDiffObj(thing, sDiffType.AdditiveType, _unpacked);
        foreach (var thing in removedList)
            _handleDiffObj(thing, sDiffType.NegativeType, _unpacked);
        foreach (var thing in unchangedList)
            setUnchangedThings(thing);

        MemUtil.LoadSnapshotProgress(0.8f, "check diff");
        RefreshTables();
        MemUtil.LoadSnapshotProgress(1.0f, "done");
    }

    void getDiffDict<T>(Dictionary<int, T> dict1, Dictionary<int, T> dict2, out List<T> addedList, out List<T> removedList, out List<T> unchangedList)
    {
        addedList = null;
        removedList = null;
        unchangedList = null;
        addedList =getExceptBtwnDict(dict1, dict2);
        removedList = getExceptBtwnDict(dict2, dict1);
        unchangedList = getIntersectBtwnDict(dict1, dict2);
    }

    //求差集 [dict1-dict2]
    List<T> getExceptBtwnDict<T>(Dictionary<int,T> dict1, Dictionary<int,T> dict2)
    {
        List<T> result = new List<T>();
        foreach (var d1 in dict1)
        {
            if(!dict2.ContainsKey(d1.Key))
            {
                result.Add(d1.Value);
            }
        }
        return result;
    }

    //求交集
    List<T> getIntersectBtwnDict<T>(Dictionary<int, T> dict1, Dictionary<int, T> dict2)
    {
        List<T> result = new List<T>();
        foreach (var d1 in dict1)
        {
            if(dict2.ContainsKey(d1.Key))
            {
                result.Add(d1.Value);
            }
        }
        return result;
    }

    void setUnchangedThings(ThingInMemory thingInMemory) 
    {
            string typeName = MemUtil.GetGroupName(thingInMemory);
            if (typeName.Length == 0)
                return ;
            int category = MemUtil.GetCategory(thingInMemory);

            MemObject item = new MemObject(thingInMemory, _unpacked);
            if (!_staticDetailInfo.isDetailStaticFileds(typeName, thingInMemory.caption, item.Size))
            {
                MemType theType;

                Dictionary<string, MemType> unchangedDict;
                _diffDicts.TryGetValue(sDiffDictKey.unchangedDict, out unchangedDict);
                if (!unchangedDict.ContainsKey(typeName))
                {
                    theType = new MemType();
                    theType.TypeName = MemUtil.GetCategoryLiteral(thingInMemory) + typeName;
                    theType.Category = category;
                    theType.Objects = new List<object>();
                    unchangedDict.Add(typeName, theType);
                }
                else
                {
                    theType = unchangedDict[typeName];
                }
                theType.AddObject(item);
            }
    }

    void _handleDiffObj(ThingInMemory thing, string diffType, CrawledMemorySnapshot resultPacked)
    {
            var theType = _checkNewTypes(thing, diffType);
            if (theType == null)
                return;
            string TypeName = MemUtil.GetGroupName(thing);

            ThingInMemory newThings = null;
            if (thing is NativeUnityEngineObject)
            {
                var nat = thing as NativeUnityEngineObject;
                var newNat = new NativeUnityEngineObject();
                newNat.caption = thing.caption;
                newNat.classID = nat.classID;
                newNat.className = TypeName + diffType;
                newNat.instanceID = nat.instanceID;
                newNat.isManager = false;
                newNat.size = thing.size;
                newNat.hideFlags = nat.hideFlags;
                newNat.isPersistent = nat.isPersistent;
                newNat.name = nat.name;
                newNat.referencedBy = thing.referencedBy;
                newNat.references = thing.references;
                newNat.isDontDestroyOnLoad = nat.isDontDestroyOnLoad;
                newThings = newNat;
            }
            else
            {
                var mao = thing as ManagedObject;
                var newMao = new ManagedObject();
                newMao.caption = TypeName + diffType;
                newMao.address = mao.address;
                newMao.referencedBy = mao.referencedBy;
                newMao.references = mao.references;
                newMao.size = mao.size;
                newMao.typeDescription = mao.typeDescription;
                newThings = newMao;
            }
            MemObject item = new MemObject(newThings, resultPacked);
            theType.AddObject(item);
    }

    Dictionary<string, MemType> _getDictByDiffType(string diffType) 
    {
        Dictionary<string, MemType> result; 
        if (string.IsNullOrEmpty(diffType))
        {
            _diffDicts.TryGetValue(sDiffDictKey.unchangedDict,out result);
            return result;
        }
            
        if(diffType.Equals(sDiffType.AdditiveType))
        {
            _diffDicts.TryGetValue(sDiffDictKey.addedDict, out result);
            return result;
        }else if(diffType.Equals(sDiffType.NegativeType))
        {
            _diffDicts.TryGetValue(sDiffDictKey.removedDict, out result);
            return result;
        }
        _diffDicts.TryGetValue(sDiffDictKey.unchangedDict, out result);
        return result;
    }

    MemType _checkNewTypes(ThingInMemory things, string diffType)
    {
        string TypeName = MemUtil.GetGroupName(things);
        if (TypeName.Length == 0 || things == null || TypeName.Contains(sDiffType.AdditiveType))
            return null;
        string diffTypeName = MemUtil.GetCategoryLiteral(things) + TypeName + diffType;
        MemType theType;
        var diffDict = _getDictByDiffType(diffType);

        if (!diffDict.ContainsKey(diffTypeName))
        {
            theType = new MemType();
            theType.TypeName = diffTypeName;
            theType.Category = MemUtil.GetCategory(things);
            theType.Objects = new List<object>();
            diffDict.Add(diffTypeName, theType);
        }
        else
        {
            theType = diffDict[diffTypeName];
        }
        return theType;
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
            return;

        List<object> qualified = new List<object>();

        // search for instances
        if (!string.IsNullOrEmpty(_searchInstanceString))
        {
            Dictionary<string, MemType> unchangedDict;
            _diffDicts.TryGetValue(sDiffDictKey.unchangedDict,out unchangedDict);
            unchangedDict.Remove(MemConst.SearchResultTypeString);
            _searchResultType = new MemType();
            _searchResultType.TypeName = MemConst.SearchResultTypeString + " " + _searchInstanceString;
            _searchResultType.Category = 0;
            _searchResultType.Objects = new List<object>();

            string search = _searchInstanceString.ToLower();
            foreach (ThingInMemory thingInMemory in _unpacked.allObjects)
            {
                if (thingInMemory.caption.ToLower().Contains(search))
                {
                    _searchResultType.AddObject(new MemObject(thingInMemory, _unpacked));
                }
            }

            unchangedDict.Add(MemConst.SearchResultTypeString, _searchResultType);
            qualified.Add(_searchResultType);
            _typeTable.RefreshData(qualified, getSpecialColorDict(qualified));
            _objectTable.RefreshData(_searchResultType.Objects);
            return;
        }

        // search for types
        if (!string.IsNullOrEmpty(_searchTypeString))
        {
            foreach(var dict in _diffDicts)
            {
                foreach (var p in dict.Value)
                {
                    MemType mt = p.Value;
                    if (mt.TypeName.ToLower().Contains(_searchTypeString.ToLower()))
                        qualified.Add(mt);
                }
            }

            _typeTable.RefreshData(qualified, getSpecialColorDict(qualified));
            _objectTable.RefreshData(null);
            return;
        }

        // ordinary case - list categorized types and instances

        foreach (var dict in _diffDicts)
        {
            foreach (var p in dict.Value)
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
                        qualified.Add(mt);
                    }
                }
            }
        }

        _typeTable.RefreshData(qualified, getSpecialColorDict(qualified));
        _objectTable.RefreshData(null);
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
            var  tempToggle = GUILayout.Toggle(_showdiffToggle, new GUIContent("show diff"), GUILayout.MaxWidth(80));
            if(tempToggle!=_showdiffToggle)
            {
               _showdiffToggle = tempToggle;
               var hostWindow = _hostWindow as MemoryProfilerWindow.MemoryProfilerWindow;
               hostWindow.RefreshCurrentView();            
            }


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

        if (GUILayout.Button("Show Type Stats"))
        {
            MemStats.ShowTypeStats(_typeTable.GetSelected() as MemType);
        }

        if (GUILayout.Button("DetailInfo", GUILayout.MinWidth(80)))
        {
            _staticDetailInfo.showInfos();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(3);

        GUILayout.BeginHorizontal(MemStyles.Toolbar);
        {
            string enteredString = GUILayout.TextField(_searchTypeString, 100, MemStyles.SearchTextField, GUILayout.MinWidth(200));
            if (enteredString != _searchTypeString)
            {
                _searchTypeString = enteredString;
                RefreshTables();
            }
            if (GUILayout.Button("", MemStyles.SearchCancelButton))
            {
                Dictionary<string, MemType> unchangedDict;
                _diffDicts.TryGetValue(sDiffDictKey.unchangedDict, out unchangedDict);
                unchangedDict.Remove(MemConst.SearchResultTypeString);
                _searchTypeString = "";
                GUI.FocusControl(null); // Remove focus if cleared
                RefreshTables();
            }
        }

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

        GUILayout.FlexibleSpace();

        // search box
        {
            string enteredString = GUILayout.TextField(_searchInstanceString, 100, MemStyles.SearchTextField, GUILayout.MinWidth(200));
            if (enteredString != _searchInstanceString)
            {
                _searchInstanceString = enteredString;
                RefreshTables();
            }
            if (GUILayout.Button("", MemStyles.SearchCancelButton))
            {
                Dictionary<string, MemType> unchangedDict;
                _diffDicts.TryGetValue(sDiffDictKey.unchangedDict, out unchangedDict);
                unchangedDict.Remove(MemConst.SearchResultTypeString);
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
            bool isMatch = false;
            foreach(var dict in _diffDicts)
            {
                if (dict.Value.TryGetValue(typeName, out mt))
                {
                    isMatch = true;
                    break;
                }
            }
            if (!isMatch)
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
