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
public enum eProfilerMode
{
    Editor,
    Remote,
    Saved,
}

namespace MemoryProfilerWindow
{
    using Item = Assets.Editor.Treemap.Item;
    using Group = Assets.Editor.Treemap.Group;
    using UnityEditorInternal;
    using System.Text.RegularExpressions;
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

        static List<string> _SnapshotOptions = new List<string>();
        static List<MemSnapshotInfo> _SnapshotChunks = new List<MemSnapshotInfo>();
        static int _selectedSnapshot = Invalid_Int;

        static string[] _ConnectedOptions = new string[] { "Editor", "Remote","Saved"};

        SnapshotIOperator _snapshotIOperator = new SnapshotIOperator();

        bool _isRemoteConnected = false;

        eProfilerMode _selectedProfilerMode;

        [SerializeField]
        string lastLoginIP ="<ip>";

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
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived += OnSnapshotReceived;
                _registered = true;
            }

            if (_tableBrowser == null)
                _tableBrowser = new MemTableBrowser(this);
            clearSnapshotChunk();
            _snapshotIOperator.reset();
        }

        public static bool isValidateIPAddress(string ipAddress)
        {
            Regex validipregex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return (ipAddress != "" && validipregex.IsMatch(ipAddress.Trim())) ? true : false;
        }


        public void connectIP(string ip) {
            if (!isValidateIPAddress(ip))
            {
                ShowNotification(new GUIContent(string.Format("Invaild IP = {0}!", ip)));
                return;
            }
            ProfilerDriver.DirectIPConnect(ip);
            if (ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID)
            {
                var content = new GUIContent(string.Format("Connecting {0} Succeeded!", ip));
                ShowNotification(content);
                _isRemoteConnected = true;
            }
            else {
                var content = new GUIContent("Connecting failed!)");
                ShowNotification(content);
                Debug.LogErrorFormat("Connection failed ip:{0}", ip);
                _isRemoteConnected = false;
            }
        }

        public void connectEditor() {
            if (ProfilerDriver.connectedProfiler == -1)
                return;
            var content = new GUIContent(string.Format("Connecting Editor Succeeded!"));
            ShowNotification(content);
            ProfilerDriver.connectedProfiler = -1;
        }


        void OnDisable()
        {
            if (_registered)
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.OnSnapshotReceived -= OnSnapshotReceived;
                _registered = false;
            }

            if (_treeMapView != null)
                _treeMapView.CleanupMeshes();
        }

        void OnSnapshotReceived(PackedMemorySnapshot snapshot)
        {
            var snapshotInfo = new MemSnapshotInfo();
            snapshotInfo.setSnapShotTime(Time.realtimeSinceStartup);
            snapshotInfo.setSnapshotPacked(snapshot);

            _SnapshotOptions.Add(_SnapshotChunks.Count.ToString());
            _SnapshotChunks.Add(snapshotInfo);

            _selectedSnapshot = _SnapshotChunks.Count - 1;
            showSnapshotInfo();
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

        public bool switchProfilerModeDialog() {
            return EditorUtility.DisplayDialog("Switch Profiler Mode", "Warning ! \n\nSwitch Profiler Mode Will Lost Current Snapshots.", "Continue", "Cancel");
        }

        public void clearSnapshotChunk(){
            _SnapshotOptions.Clear();
            _SnapshotChunks.Clear();
            _snapshotIOperator.reset();
            _tableBrowser.clearTableData();
         }

        void drawProfilerModeGUI() {
            switch (_selectedProfilerMode)
            {
                case eProfilerMode.Editor:
                {
                    takeSnapshotBtn();
                    drawSnapshotChunksGrid(320,780);
                    GUILayout.FlexibleSpace();
                    //save 
                    saveSessionBtn();
                }
                break;
                case eProfilerMode.Remote:
                {
                    var currentStr = GUILayout.TextField(lastLoginIP, GUILayout.Width(80));
                    if (!lastLoginIP.Equals(currentStr))
                    {
                        lastLoginIP = currentStr;
                        _isRemoteConnected = false;
                    }

                    bool savedState = GUI.enabled;
                    if (_isRemoteConnected)
                    {
                        GUI.enabled = false;
                    }

                    if (GUILayout.Button("Connect", GUILayout.Width(60)))
                    {
                        connectIP(lastLoginIP);
                    }
                    GUI.enabled = savedState;

                    takeSnapshotBtn();
                    // add time point snapshots
                    drawSnapshotChunksGrid(470,630);
                    GUILayout.FlexibleSpace();
                    //save 
                    saveSessionBtn();
                }        
                break;
                case eProfilerMode.Saved:
                    drawSnapshotChunksGrid(210,930);
                    GUILayout.FlexibleSpace();
                    //load
                    loadSessionBtn();

                    break;
                default:
                    break;
            }
        }

        private void drawSnapshotChunksGrid(int startPosX,int width)
        {
            var snapShotOptArray = _SnapshotOptions.ToArray();
            var currentIndex = GUI.SelectionGrid(new Rect(startPosX, 0, 30 * snapShotOptArray.Length, 20), _selectedSnapshot, snapShotOptArray
                , snapShotOptArray.Length, MemStyles.ToolbarButton);
            if (currentIndex != Invalid_Int && currentIndex != _selectedSnapshot)
            {
                _selectedSnapshot = currentIndex;
                showSnapshotInfo();
            }
        }

        private  void takeSnapshotBtn()
        {
            bool savedState = GUI.enabled;
            if (_selectedProfilerMode == eProfilerMode.Remote && ProfilerDriver.connectedProfiler != PLAYER_DIRECT_IP_CONNECT_GUID)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
            {
                requestSnapshot();
            }
            GUI.enabled = savedState;
        }

        private void requestSnapshot()
        {
            UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
        }

        void OnGUI()
        {
            // main bar
            {
                GUILayout.BeginHorizontal();
                int connectedIndex = GUI.SelectionGrid(new Rect(0, 0,180, 20),(int)_selectedProfilerMode, _ConnectedOptions, _ConnectedOptions.Length, MemStyles.ToolbarButton);
                if (connectedIndex != (int)_selectedProfilerMode)
                {
                    _isRemoteConnected = false;

                    if (_SnapshotChunks.Count > 0 
                        && !_snapshotIOperator.isSaved(_SnapshotChunks.Count, _selectedProfilerMode, lastLoginIP)
                        && !switchProfilerModeDialog())
                    {
                    }
                    else
                    {
                        clearSnapshotChunk();
                        _selectedProfilerMode = (eProfilerMode)connectedIndex;

                        if (_selectedProfilerMode == (int)eProfilerMode.Editor)
                            connectEditor();
                    }
                }

                GUILayout.Space(200);
                drawProfilerModeGUI();
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

        private void loadSessionBtn()
        {
            if (GUILayout.Button("Load Session", GUILayout.MaxWidth(100)))
            {
                List<object> packeds;
                var isSuc = _snapshotIOperator.loadSnapshotMemPacked(out packeds);
                if (!isSuc || packeds.Count == 0)
                {
                    ShowNotification(new GUIContent("Load Snapshots Failed!"));
                }
                else
                {
                    clearSnapshotChunk();
                    ShowNotification(new GUIContent("load snapshots succeeded!"));
                    foreach (var obj in packeds)
                    {
                        _SnapshotOptions.Add(_SnapshotChunks.Count.ToString());
                        _SnapshotChunks.Add(obj as MemSnapshotInfo);
                    }
                }
            }
        }

        private void saveSessionBtn()
        {
            if (GUILayout.Button("Save Session", GUILayout.MaxWidth(100)))
            {
                if (_snapshotIOperator.saveAllSnapshot(_SnapshotChunks,_selectedProfilerMode,lastLoginIP))
                {
                    var content = new GUIContent(string.Format("Save snapshots Succeeded!"));
                    ShowNotification(content);
                }
                else
                {
                    var content = new GUIContent(string.Format("Save snapshots Failed!"));
                    ShowNotification(content);
                }
            }
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

        void showSnapshotInfo() 
        {
            var curSnapShotChunk =_SnapshotChunks[_selectedSnapshot];
            if (_selectedSnapshot >= 1)
            {
                MemSnapshotInfo preSnapShotChunk = _SnapshotChunks[_selectedSnapshot - 1];
                if (preSnapShotChunk != null)
                {
                    var tempPrePackedCrawled = new Crawler().Crawl(preSnapShotChunk.snapshot);
                    _preUnpackedCrawl = CrawlDataUnpacker.Unpack(tempPrePackedCrawled);
                }
            }
            else
            {
                _preUnpackedCrawl = null;
            }

            _snapshot = curSnapShotChunk.snapshot;

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
