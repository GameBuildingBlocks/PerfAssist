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

namespace MemoryProfilerWindow
{
    using Item = Assets.Editor.Treemap.Item;
    using Group = Assets.Editor.Treemap.Group;
    public class MemoryProfilerWindow : EditorWindow
    {
        public static int Invalid_Int = -1;

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

        float _trackerStartTime = Invalid_Int;

        public static List<string> _SnapshotOptions = new List<string>();
        public static List<PackedMemorySnapshot> _SnapshotChunk = new List<PackedMemorySnapshot>();
        public static int _SnapshotChunkIndex = Invalid_Int;

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
            _SnapshotOptions.Clear();
            _SnapshotChunk.Clear();
        }


        void OnGUI()
        {
            // main bar
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Take Snapshot", GUILayout.Width(150)))
                {
                    UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
                }

                // add time point snapshots
                var snapShotOptArray = _SnapshotOptions.ToArray();
                int currentIndex = GUI.SelectionGrid(new Rect(200, 0, 900, 20),_SnapshotChunkIndex,snapShotOptArray,snapShotOptArray.Length);
                if (currentIndex != Invalid_Int && currentIndex != _SnapshotChunkIndex)
                {
                    _SnapshotChunkIndex = currentIndex;
                    showSnapshotInfo();
                }

                GUILayout.FlexibleSpace();
                //save 
                if (GUILayout.Button("Save Snapshot",GUILayout.MaxWidth(100)))
                {
                    EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
                }

                //load
                if (GUILayout.Button("Load Snapshot", GUILayout.MaxWidth(100)))
                {
                    EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
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
            var optTime = Time.realtimeSinceStartup;
            if (_trackerStartTime == Invalid_Int)
                _trackerStartTime = optTime;
            _SnapshotOptions.Add((optTime - _trackerStartTime).ToString());
            _SnapshotChunk.Add(snapshot);
        }

        void showSnapshotInfo() {
            var curSnapShotChunk =_SnapshotChunk[_SnapshotChunkIndex];
            PackedMemorySnapshot preSnapShotChunk = null;
            if (_SnapshotChunkIndex >= 1)
            {
                preSnapShotChunk = _SnapshotChunk[_SnapshotChunkIndex-1];
            }
            IncomingSnapshotForCompare(curSnapShotChunk,preSnapShotChunk);
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
