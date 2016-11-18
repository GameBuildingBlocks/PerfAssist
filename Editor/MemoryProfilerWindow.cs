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
        [NonSerialized]
        UnityEditor.MemoryProfiler.PackedMemorySnapshot _snapshot;

        [SerializeField]
        PackedCrawlerData _packedCrawled;

        [NonSerialized]
        CrawledMemorySnapshot _unpackedCrawl;

        Vector2 _scrollPosition;

        [NonSerialized]
        private bool _registered = false;

        Inspector _inspector;
        TreeMapView _treeMapView;
        MemTableBrowser _tableBrowser;

        ThingInMemory _selectedThing;

        public bool EnhancedMode { get { return _enhancedMode; } }
        bool _enhancedMode = true;

        bool _autoSaveForComparison = false;
        int _selectedBegin = 0;
        int _selectedEnd = 0;
        eShowType m_selectedView = 0;
        string[] _snapshotFiles = new string[] { };

        [MenuItem("Window/PerfAssist/MemoryProfiler")]
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
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived += IncomingSnapshot;
                _registered = true;
            }

            if (_tableBrowser == null)
                _tableBrowser = new MemTableBrowser(this);

            RefreshSnapshotList();
        }

        void OnDisable()
        {
            if (_registered)
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived -= IncomingSnapshot;
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

        void OnGUI()
        {
            GUILayout.BeginHorizontal();

            _enhancedMode = GUILayout.Toggle(_enhancedMode, "Enhanced Mode", GUILayout.MaxWidth(150));

            if (GUILayout.Button("Take Snapshot"))
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();

                // the above call (RequestNewSnapshot) is a sync-invoke so we can process it immediately
                if (_enhancedMode && _autoSaveForComparison)
                {
                    string filename = MemUtil.Save(_snapshot);
                    if (!string.IsNullOrEmpty(filename))
                    {
                        Debug.LogFormat("snapshot '{0}' saved.", filename);

                        RefreshSnapshotList();
                    }
                }
            }

            if (_enhancedMode)
            {
                _autoSaveForComparison = GUILayout.Toggle(_autoSaveForComparison, "Auto-Save");
            }

            if (GUILayout.Button("Save Snapshot..."))
            {
                if (_snapshot != null)
                {
                    string fileName = EditorUtility.SaveFilePanel("Save Snapshot", null, "MemorySnapshot", "memsnap");
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        using (Stream stream = File.Open(fileName, FileMode.Create))
                        {
                            bf.Serialize(stream, _snapshot);
                        }
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning("No snapshot to save.  Try taking a snapshot first.");
                }
            }
            if (GUILayout.Button("Load Snapshot..."))
            {
                string fileName = EditorUtility.OpenFilePanel("Load Snapshot", null, "memsnap");
                if (!string.IsNullOrEmpty(fileName))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    using (Stream stream = File.Open(fileName, FileMode.Open))
                    {
                        IncomingSnapshot(bf.Deserialize(stream) as PackedMemorySnapshot);
                    }
                }
            }

            if (_enhancedMode)
            {
                GUILayout.FlexibleSpace();

                {
                    GUILayout.Space(50);
                    EditorGUIUtility.labelWidth = 40;
                    _selectedBegin = EditorGUILayout.Popup(string.Format("Begin"), _selectedBegin, _snapshotFiles, GUILayout.Width(250));

                    GUILayout.Space(50);

                    _selectedEnd = EditorGUILayout.Popup(string.Format("End"), _selectedEnd, _snapshotFiles, GUILayout.Width(250));
                    EditorGUIUtility.labelWidth = 0; // reset to default
                    GUILayout.Space(50);
                }

                if (_selectedBegin == _selectedEnd)
                {
                    GUI.enabled = false;
                }
                if (GUILayout.Button("Compare", GUILayout.MaxWidth(120)))
                {
                    Debug.LogFormat("Compare '{0}' with '{1}'", _snapshotFiles[_selectedBegin], _snapshotFiles[_selectedEnd]);

                    var snapshotBegin = MemUtil.Load(_snapshotFiles[_selectedBegin]);
                    var snapshotEnd = MemUtil.Load(_snapshotFiles[_selectedEnd]);

                    if (snapshotBegin != null && snapshotEnd != null)
                    {
                        MemCompareTarget.Instance.SetCompareTarget(snapshotBegin);

                        IncomingSnapshot(snapshotEnd);

                        if (_treeMapView != null)
                            _treeMapView.Setup(this, _unpackedCrawl, MemCompareTarget.Instance.GetNewlyAdded(_unpackedCrawl));
                    }
                }
                if (_selectedBegin == _selectedEnd)
                {
                    GUI.enabled = true;
                }

                if (GUILayout.Button("Open Dir", GUILayout.MaxWidth(120)))
                {
                    EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
                }
            }

            GUILayout.EndHorizontal();

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

            //RenderDebugList();
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

        private void RenderDebugList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            foreach (var thing in _unpackedCrawl.allObjects)
            {
                var mo = thing as ManagedObject;
                if (mo != null)
                    GUILayout.Label("MO: " + mo.typeDescription.name);

                var gch = thing as GCHandle;
                if (gch != null)
                    GUILayout.Label("GCH: " + gch.caption);

                var sf = thing as StaticFields;
                if (sf != null)
                    GUILayout.Label("SF: " + sf.typeDescription.name);
            }

            GUILayout.EndScrollView();
        }

        void IncomingSnapshot(PackedMemorySnapshot snapshot)
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

        void RefreshSnapshotList()
        {
            _snapshotFiles = MemUtil.GetFiles();
        }

        void RefreshCurrentView()
        {
            if (_unpackedCrawl == null)
                return;

            switch (m_selectedView)
            {
                case eShowType.InTable:
                    if (_tableBrowser != null)
                        _tableBrowser.RefreshData(_unpackedCrawl);
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
