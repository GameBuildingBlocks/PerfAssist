using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.MemoryProfiler;

enum eShowType
{
    InTable,
    InTreemap,
}
enum eConnectedType
{
    Editor,
    Remote,
}

namespace MemoryProfilerWindow
{
    using Item = Assets.Editor.Treemap.Item;
    using Group = Assets.Editor.Treemap.Group;
    using UnityEditorInternal;
    public class MemoryProfilerWindow : EditorWindow
    {
        public static int Invalid_Int = -1;
        private const int PLAYER_DIRECT_IP_CONNECT_GUID = 65261;
        [NonSerialized]
        UnityEditor.MemoryProfiler.PackedMemorySnapshot _snapshot;

        [SerializeField]
        PackedCrawlerData _packedCrawled;

        [NonSerialized]
        CrawledMemorySnapshot _unpackedCrawl;
        CrawledMemorySnapshot _preUnpackedCrawl;

        [NonSerialized]
        private bool _registered = false;

        Inspector _inspector;
        TreeMapView _treeMapView;
        MemTableBrowser _tableBrowser;

        ThingInMemory _selectedThing;

        eShowType m_selectedView = 0;

        int _snapshotIndex = 0;

        public static List<string> _SnapshotOptions = new List<string>();
        public static List<MemSnapshotInfo> _SnapshotChunk = new List<MemSnapshotInfo>();
        public static int _SnapshotChunkIndex = Invalid_Int;

        public static string [] _ConnectedOptions = new string []{"editor","remote"};
        public static int _connectedIndex =0;

        SnapshotIOperator _snapshotIOperator = new SnapshotIOperator();
        
        [SerializeField]
        string lastLoginIP ="10.20.90.32";


        [MenuItem("Window/PerfAssist/ResourceTracker")]
        static void Create()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>();
        }

        void OnEnable()
        {
            if (_treeMapView == null)
                _treeMapView = new TreeMapView();

            if (!_registered)
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived += IncomingSnapshotByBtn;
                _registered = true;
            }

            if (_tableBrowser == null)
                _tableBrowser = new MemTableBrowser(this);
            clearSnapshotChunk();
            _snapshotIOperator.reset();
        }

        public void connectedIP(string ip) {
            if (!ip.Equals(""))
            {
                ProfilerDriver.DirectIPConnect(ip);
                if (ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID)
                {
                    var content = new GUIContent("Connecting succeeed!)");
                    ShowNotification(content);
                }
                else {
                    var content = new GUIContent("Connecting failed!)");
                    ShowNotification(content);
                    Debug.LogErrorFormat("connected failed ip:{0}", ip);                    
                }
            }
        }

        public void connectedNative() {
            ProfilerDriver.connectedProfiler =-1;
        }


        void OnDisable()
        {
            if (_registered)
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived -= IncomingSnapshotByBtn;
                _registered = false;
            }

            if (_treeMapView != null)
                _treeMapView.CleanupMeshes();
        }

        void Update()
        {
            // the selecting should be performed outside OnGUI() to prevent exception below:
            //      ArgumentException: control 1's position in group with only 1 control
            //  http://answers.unity3d.com/questions/240913/argumentexception-getting-control-1s-position-in-a.html
            //  http://answers.unity3d.com/questions/400454/argumentexception-getting-control-0s-position-in-a-1.html
            if (_inspector != null && _selectedThing != _inspector.Selected)
            {
                switch (m_selectedView)
                {
                    case eShowType.InTable:
                        if (_tableBrowser != null)
                            _tableBrowser.SelectThing(_selectedThing);
                        break;
                    case eShowType.InTreemap:
                        if (_treeMapView != null)
                            _treeMapView.SelectThing(_selectedThing);
                        break;
                    default:
                        break;
                }
                if (_inspector != null)
                    _inspector.SelectThing(_selectedThing);
            }
        }

        public void clearSnapshotChunk(){
            _snapshotIndex = 0;
            _SnapshotOptions.Clear();
            _SnapshotChunk.Clear();
        }


        void OnGUI()
        {
            // main bar
            {
                GUILayout.BeginHorizontal();
                int connectedIndex = GUI.SelectionGrid(new Rect(0, 0,120, 20), _connectedIndex, _ConnectedOptions, _ConnectedOptions.Length);
                if (connectedIndex != _connectedIndex)
                {
                    _connectedIndex = connectedIndex;
                    if (_connectedIndex == (int)eConnectedType.Editor)
                        connectedNative();
                }
                GUILayout.Space(130);
                if (connectedIndex == (int)eConnectedType.Remote)
                {
                    var currentStr = GUILayout.TextField(lastLoginIP, GUILayout.Width(80));
                    if (!lastLoginIP.Equals(currentStr))
                    {
                        lastLoginIP = currentStr;
                    }

                    if (GUILayout.Button("connect", GUILayout.Width(60)))
                    {
                        connectedIP(lastLoginIP);
                    }
                }

                if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
                {
                    UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
                }

                // add time point snapshots
                var snapShotOptArray = _SnapshotOptions.ToArray();
                int gridPosX = 250;
                int gridWidth = 820;
                if (_connectedIndex == (int)eConnectedType.Remote)
                {
                    gridPosX += 150;
                    gridWidth -= 150;
                }

                int currentIndex = GUI.SelectionGrid(new Rect(gridPosX, 0,gridWidth, 20), _SnapshotChunkIndex, snapShotOptArray, snapShotOptArray.Length);
                if (currentIndex != Invalid_Int && currentIndex != _SnapshotChunkIndex)
                {
                    _SnapshotChunkIndex = currentIndex;
                    showSnapshotInfo();
                }

                GUILayout.FlexibleSpace();
                //save 
                if (GUILayout.Button("Save Session",GUILayout.MaxWidth(100)))
                {
                    if (_snapshotIOperator.saveAllSnapshot(_SnapshotChunk))
                    {
                        var content = new GUIContent(string.Format("save all snapshots successed!"));
                        ShowNotification(content);
                    }
                    else {
                        var content = new GUIContent(string.Format("save all snapshots failed!"));
                        ShowNotification(content);
                    }
                }

                //load
                if (GUILayout.Button("Load Session", GUILayout.MaxWidth(100)))
                {
                    List<object> packeds;
                    var isSuc = _snapshotIOperator.loadSnapshotMemPacked(out packeds);
                    if (!isSuc)
                    {
                        var content = new GUIContent(string.Format("load snapshots failed!"));
                        ShowNotification(content);
                    }
                    else {
                        if (packeds.Count > 0)
                        {
                            clearSnapshotChunk();
                            var content = new GUIContent(string.Format("load snapshots successed!"));
                            ShowNotification(content);
                        }
                        foreach (var obj in packeds)
                        {
                            IncomingSnapshotByLoad(obj as MemSnapshotInfo);
                        }
                    }
                }

                if (GUILayout.Button("Open Dir", GUILayout.MaxWidth(100)))
                {
                    EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
                }
                GUILayout.EndHorizontal();
            }

            // view bar
            GUILayout.BeginArea(new Rect(0, MemConst.TopBarHeight, position.width - MemConst.InspectorWidth, 30));
            GUILayout.BeginHorizontal(MemStyles.Toolbar);
            int selected = GUILayout.SelectionGrid((int)m_selectedView, MemConst.ShowTypes, MemConst.ShowTypes.Length, MemStyles.ToolbarButton);
            if (m_selectedView != (eShowType)selected)
            {
                m_selectedView = (eShowType)selected;
                RefreshCurrentView();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // selected views
            float TabHeight = 30;
            float yoffset = MemConst.TopBarHeight + TabHeight;
            Rect view = new Rect(0f, yoffset, position.width - MemConst.InspectorWidth, position.height - yoffset);
            switch (m_selectedView)
            {
                case eShowType.InTable:
                    if (_tableBrowser != null)
                        _tableBrowser.Draw(view);
                    break;

                case eShowType.InTreemap:
                    if (_treeMapView != null)
                        _treeMapView.Draw(view);
                    break;

                default:
                    break;
            }

            if (_inspector != null)
                _inspector.Draw();
        }

        public string[] FindThingsByName(string name)
        {
            string lower = name.ToLower();
            List<string> ret = new List<string>();
            foreach (var thing in _unpackedCrawl.allObjects)
            {
                var nat = thing as NativeUnityEngineObject;
                if (nat != null && nat.name.ToLower().Contains(lower))
                    ret.Add(string.Format("({0})/{1}", nat.className, nat.name));
            }
            return ret.ToArray();
        }

        public ThingInMemory FindThingInMemoryByExactName(string name)
        {
            foreach (var thing in _unpackedCrawl.allObjects)
            {
                var nat = thing as NativeUnityEngineObject;
                if (nat != null && nat.name == name)
                    return thing;
            }

            return null;
        }

        public void SelectThing(ThingInMemory thing)
        {
            _selectedThing = thing;
        }

        public void SelectGroup(Group group)
        {
            switch (m_selectedView)
            {
                case eShowType.InTable:
                    break;
                case eShowType.InTreemap:
                    if (_treeMapView != null)
                        _treeMapView.SelectGroup(group);
                    break;
                default:
                    break;
            }
        }

        void addNewSnapshotBtn(PackedMemorySnapshot snapshot)
        {
            var snapshotInfo =new MemSnapshotInfo();
            snapshotInfo.setSnapShotTime(Time.realtimeSinceStartup);
            snapshotInfo.setSnapshotPacked(snapshot);
            

            _SnapshotOptions.Add(_snapshotIndex.ToString());
            _snapshotIndex++;
            _SnapshotChunk.Add(snapshotInfo);
        }

        void showSnapshotInfo() {
            var curSnapShotChunk =_SnapshotChunk[_SnapshotChunkIndex];
            MemSnapshotInfo preSnapShotChunk = null;
            if (_SnapshotChunkIndex >= 1)
            {
                preSnapShotChunk = _SnapshotChunk[_SnapshotChunkIndex - 1];
                IncomingSnapshotForCompare(curSnapShotChunk.snapshot, preSnapShotChunk.snapshot);
            }
            else {
                IncomingSnapshotForCompare(curSnapShotChunk.snapshot);            
            }
        }

        void IncomingSnapshotByLoad(MemSnapshotInfo snapshotInfo)
        {
            _SnapshotOptions.Add(_snapshotIndex.ToString());
            _snapshotIndex++;
            _SnapshotChunk.Add(snapshotInfo);
        }

        void IncomingSnapshotByBtn(PackedMemorySnapshot snapshot)
        {
            addNewSnapshotBtn(snapshot);
        }

        void IncomingSnapshotForCompare(PackedMemorySnapshot snapshot, PackedMemorySnapshot preSnapshot = null)
        {
            if (preSnapshot != null)
            {
                var tempPrePackedCrawled = new Crawler().Crawl(preSnapshot);
                _preUnpackedCrawl = CrawlDataUnpacker.Unpack(tempPrePackedCrawled);
            }
            else
            {
                _preUnpackedCrawl = null;
            }
            _IncomingSnapshot(snapshot);
        }

        void _IncomingSnapshot(PackedMemorySnapshot snapshot)
        {
            _snapshot = snapshot;

            MemUtil.LoadSnapshotProgress(0.01f, "creating Crawler");

            _packedCrawled = new Crawler().Crawl(_snapshot);
            MemUtil.LoadSnapshotProgress(0.7f, "unpacking");

            _unpackedCrawl = CrawlDataUnpacker.Unpack(_packedCrawled);
            MemUtil.LoadSnapshotProgress(0.8f, "creating Inspector");

            _inspector = new Inspector(this, _unpackedCrawl, _snapshot);
            MemUtil.LoadSnapshotProgress(0.9f, "refreshing view");

            RefreshCurrentView();
            MemUtil.LoadSnapshotProgress(1.0f, "done");
        }

        void RefreshCurrentView()
        {
            if (_unpackedCrawl == null)
                return;

            switch (m_selectedView)
            {
                case eShowType.InTable:
                    if (_tableBrowser != null)
                        _tableBrowser.RefreshData(_unpackedCrawl,_preUnpackedCrawl);
                    break;
                case eShowType.InTreemap:
                    if (_treeMapView != null)
                        _treeMapView.Setup(this, _unpackedCrawl);
                    break;
                default:
                    break;
            }
        }
    }
}
