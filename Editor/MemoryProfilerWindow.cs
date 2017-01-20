using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MemoryProfiler;
using UnityEditorInternal;
using UnityEngine;
using Group = Assets.Editor.Treemap.Group;


enum eShowType
{
    InTable,
    InTreemap,
}
public enum eProfilerMode
{
    Editor,
    Remote,
    File,
}

namespace MemoryProfilerWindow
{
    public class MemoryProfilerWindow : EditorWindow
    {
        [NonSerialized]
        CrawledMemorySnapshot _unpackedCrawl;
        public CrawledMemorySnapshot UnpackedCrawl
        {
            get { return _unpackedCrawl; }
        }

        CrawledMemorySnapshot _preUnpackedCrawl;

        Inspector _inspector;
        TreeMapView _treeMapView;
        MemTableBrowser _tableBrowser;

        ThingInMemory _selectedThing;

        eShowType m_selectedView = 0;

        static List<string> _SnapshotOptions = new List<string>();

        static List<MemSnapshotInfo> _nativeSnapshotSessions = new List<MemSnapshotInfo>();
        static List<MemSnapshotInfo> _remoteSnapshotSessions = new List<MemSnapshotInfo>();

        static int _selectedSnapshot = PAEditorConst.BAD_ID;

        SnapshotIOperator _snapshotIOperator = new SnapshotIOperator();

        eProfilerMode _selectedProfilerMode = eProfilerMode.Editor;

        string _IPField = MemConst.RemoteIPDefaultText;

        StackInfoSynObj _stackInfoObj = new StackInfoSynObj();

        bool _autoSaveToggle = true;

        TrackerModeManager _modeMgr = new TrackerModeManager();

        [MenuItem(PAEditorConst.MenuPath + "/ResourceTracker")]
        static void Create()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>();
        }

        MemoryProfilerWindow()
        {
            _modeMgr.SetSelectionChanged(OnSnapshotSelectionChanged);
        }

        void Awake()
        {
            if (_treeMapView == null)
                _treeMapView = new TreeMapView();

            if (_tableBrowser == null)
                _tableBrowser = new MemTableBrowser(this);

            MemorySnapshot.OnSnapshotReceived -= OnSnapshotReceived;
            MemorySnapshot.OnSnapshotReceived += OnSnapshotReceived;

            PANetDrv.Instance = new PANetDrv();
            NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_QueryStacksResponse, Handle_QueryStacksResponse);

            _IPField = EditorPrefs.GetString("ResourceTrackerLastConnectedIP");
        }

        void OnDestroy()
        {
            if (_treeMapView != null)
                _treeMapView.CleanupMeshes();

            if (PANetDrv.Instance != null)
            {
                PANetDrv.Instance.Dispose();
                PANetDrv.Instance = null;
            }

            MemorySnapshot.OnSnapshotReceived -= OnSnapshotReceived;
        }

        public void Connect(string ip)
        {
            ProfilerDriver.connectedProfiler = -1;
            if (NetManager.Instance.IsConnected)
                NetManager.Instance.Disconnect();

            try
            {
                if (!MemUtil.ValidateIPString(ip))
                    throw new Exception("Invaild IP");

                if (!NetManager.Instance.Connect(ip))
                    throw new Exception("Bad Connect");

                if (!MemUtil.IsLocalhostIP(ip))
                {
                    ProfilerDriver.DirectIPConnect(ip);
                    if (!MemUtil.IsProfilerConnectedRemotely)
                        throw new Exception("Bad Connect");
                }

                EditorPrefs.SetString("ResourceTrackerLastConnectedIP", ip);
            }
            catch (Exception ex)
            {
                ShowNotification(new GUIContent(string.Format("Connecting '{0}' failed: {1}", ip, ex.Message)));
                Debug.LogException(ex);

                ProfilerDriver.connectedProfiler = -1;
                if (NetManager.Instance.IsConnected)
                    NetManager.Instance.Disconnect();
            }
        }

        void RefreshSnapshotOptions() 
        {
            _SnapshotOptions.Clear();
            var snapshotSession = getCurrentSnapshotSessionList();
            if (snapshotSession != null)
            {
                for (int i = 0; i < snapshotSession.Count; i++)
                {
                    _SnapshotOptions.Add(i.ToString());
                }
            }
        }

        void OnSnapshotReceived(PackedMemorySnapshot snapshot)
        {
            var snapshotInfo = new MemSnapshotInfo();
            snapshotInfo.setSnapShotTime(Time.realtimeSinceStartup);

            var snapshotSession = getCurrentSnapshotSessionList();

            snapshotSession.Add(snapshotInfo);
            RefreshSnapshotOptions();

            _selectedSnapshot = snapshotSession.Count - 1;
            showSnapshotInfo(snapshot);
        }

        void Update()
        {
            // the selecting should be performed outside OnGUI() to prevent exception below:
            //      ArgumentException: control 1's position in group with only 1 control
            //  http://answers.unity3d.com/questions/240913/argumentexception-getting-control-1s-position-in-a.html
            //  http://answers.unity3d.com/questions/400454/argumentexception-getting-control-0s-position-in-a-1.html
            if (_inspector != null)
            {
                if (_selectedThing != _inspector.Selected)
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

                if (_stackInfoObj.ReaderNewMsgArrived)
                {
                    _inspector._stackInfo = _stackInfoObj.readStackInfo();
                    Repaint();
                }
            }
        }

        void drawProfilerModeGUI()
        {
            switch (_selectedProfilerMode)
            {
                case eProfilerMode.Editor:
                    {
                        takeSnapshotBtn();
                        drawSnapshotChunksGrid(320, 780);
                        GUILayout.FlexibleSpace();
                        saveSessionBtn();
                    }
                    break;
                case eProfilerMode.Remote:
                    {
                        GUI.SetNextControlName("LoginIPTextField");
                        var currentStr = GUILayout.TextField(_IPField, GUILayout.Width(80));
                        if (!_IPField.Equals(currentStr))
                        {
                            _IPField = currentStr;
                        }

                        if (GUI.GetNameOfFocusedControl().Equals("LoginIPTextField") && _IPField.Equals(MemConst.RemoteIPDefaultText))
                        {
                            _IPField = "";
                        }

                        bool savedState = GUI.enabled;
                        if (NetManager.Instance.IsConnected && MemUtil.IsProfilerConnectedRemotely)
                        {
                            GUI.enabled = false;
                        }

                        if (GUILayout.Button("Connect", GUILayout.Width(60)))
                        {
                            Connect(_IPField);
                        }
                        GUI.enabled = savedState;

                        takeSnapshotBtn();
                        drawSnapshotChunksGrid(470, 630);
                        GUILayout.FlexibleSpace();
                        saveSessionBtn();
                    }
                    break;

                case eProfilerMode.File:
                    _modeMgr.OnGUI();
                    break;

                default:
                    break;
            }
        }

        private void drawSnapshotChunksGrid(int startPosX, int width)
        {
            var snapShotOptArray = _SnapshotOptions.ToArray();
            var currentIndex = GUI.SelectionGrid(new Rect(startPosX, 0, 30 * snapShotOptArray.Length, 20), _selectedSnapshot, snapShotOptArray
                , snapShotOptArray.Length, MemStyles.ToolbarButton);
            if (currentIndex != PAEditorConst.BAD_ID && currentIndex != _selectedSnapshot)
            {
                _selectedSnapshot = currentIndex;
                showSnapshotInfo();
            }
        }

        private void takeSnapshotBtn()
        {
            bool savedState = GUI.enabled;
            if (_selectedProfilerMode == eProfilerMode.Remote && MemUtil.IsProfilerConnectedRemotely)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
            {
                MemorySnapshot.RequestNewSnapshot();
            }
            GUI.enabled = savedState;
        }


        void OnGUI()
        {
            try
            {
                // main bar
                GUILayout.BeginHorizontal();
                int connectedIndex = GUI.SelectionGrid(new Rect(0, 0, 180, 20), (int)_selectedProfilerMode,
                    MemConst.ConnectionOptions,
                    MemConst.ConnectionOptions.Length,
                    MemStyles.ToolbarButton);
                if (connectedIndex != (int)_selectedProfilerMode)
                {
                    _selectedProfilerMode = (eProfilerMode)connectedIndex;

                    if (_selectedProfilerMode == (int)eProfilerMode.Editor)
                        Connect(MemConst.LocalhostIP);
                }

                GUILayout.Space(200);
                drawProfilerModeGUI();

                if (GUILayout.Button("Clear Session", GUILayout.MaxWidth(100)))
                {
                    var currentSnapshotSessions = getCurrentSnapshotSessionList();
                    if (currentSnapshotSessions.Count > 0 && EditorUtility.DisplayDialog("Clear Session", "All snapshots in current session would be removed, continue?", "Continue", "Cancel"))
                    {
                        _SnapshotOptions.Clear();
                        if (currentSnapshotSessions != null)
                            currentSnapshotSessions.Clear();
                        _tableBrowser.clearTableData();
                        _snapshotIOperator.refreshRecordTime();
                    }
                }

                if (GUILayout.Button("Open Dir", GUILayout.MaxWidth(80)))
                {
                    EditorUtility.RevealInFinder(MemUtil.SnapshotsDir);
                }
                GUILayout.EndHorizontal();

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
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void saveSessionBtn()
        {
            _autoSaveToggle = GUILayout.Toggle(_autoSaveToggle, new GUIContent("AutoSave"), GUILayout.MaxWidth(80));
        }

        public void SelectThing(ThingInMemory thing)
        {
            _selectedThing = thing;
            return;
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

        void showSnapshotInfo(PackedMemorySnapshot packed = null)
        {
            var currentSession = getCurrentSnapshotSessionList()[_selectedSnapshot];
            if (currentSession.unPacked == null && packed != null)
            {
                MemUtil.LoadSnapshotProgress(0.01f, "creating Crawler");
                var packedCrawled = new Crawler().Crawl(packed);
                MemUtil.LoadSnapshotProgress(0.7f, "unpacking");

                currentSession.unPacked = CrawlDataUnpacker.Unpack(packedCrawled);
                MemUtil.LoadSnapshotProgress(1.0f, "done");

                if (_autoSaveToggle)
                {
                    MemUtil.LoadSnapshotProgress(0.01f, "auto save session and json");
                    bool saveSessionSuc = _snapshotIOperator.saveSnapshotSessions(packed, _selectedSnapshot, _selectedProfilerMode, _IPField);
                    MemUtil.LoadSnapshotProgress(0.7f, "save session successed");
                    bool saveJsonSuc =_snapshotIOperator.saveSnapshotJsonFile(currentSession.unPacked, _selectedSnapshot, _selectedProfilerMode, _IPField);
                    MemUtil.LoadSnapshotProgress(0.9f, "save json successed");
                    if (saveSessionSuc && saveJsonSuc)
                    {
                        var content = new GUIContent(string.Format("Save Snapshots Sessions And Json Succeeded!"));
                        ShowNotification(content);
                    }
                    else
                    {
                        var content = new GUIContent(string.Format("Save Snapshots Sessions And Json Failed!"));
                        ShowNotification(content);
                    }
                    MemUtil.LoadSnapshotProgress(1.0f, "done");
                }
            }

            _unpackedCrawl = currentSession.unPacked;

            if (_selectedSnapshot >= 1)
            {
                MemSnapshotInfo preSnapShotChunk = getCurrentSnapshotSessionList()[_selectedSnapshot - 1];
                if (preSnapShotChunk != null)
                    _preUnpackedCrawl = preSnapShotChunk.unPacked;
            }
            else
            {
                _preUnpackedCrawl = null;
            }
            _inspector = new Inspector(this, _unpackedCrawl);
            RefreshCurrentView();
            Repaint();
        }

        private bool Handle_QueryStacksResponse(eNetCmd cmd, UsCmd c)
        {
            var stackInfo = c.ReadString();
            if (string.IsNullOrEmpty(stackInfo))
                return false;

            _stackInfoObj.writeStackInfo(stackInfo);
            NetUtil.Log("stack info{0}", stackInfo);
            return true;
        }

        List<MemSnapshotInfo> getCurrentSnapshotSessionList(){
            switch (_selectedProfilerMode)
            {
                case eProfilerMode.Editor:
                    {
                        return _nativeSnapshotSessions;
                    }
                case eProfilerMode.Remote:
                    {
                        return _remoteSnapshotSessions;
                    }
            }
            return null;
        }

        public void RefreshCurrentView()
        {
            if (_unpackedCrawl == null)
                return;

            switch (m_selectedView)
            {
                case eShowType.InTable:
                    if (_tableBrowser != null)
                        if (_tableBrowser._showdiffToggle && getCurrentSnapshotSessionList().Count >= 2)
                            _tableBrowser.RefreshDiffData(_unpackedCrawl, _preUnpackedCrawl);
                        else
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

        private void OnSnapshotSelectionChanged()
        {
            _unpackedCrawl = _modeMgr.SelectedUnpacked;
            _preUnpackedCrawl = _modeMgr.PrevUnpacked;
            _inspector = new Inspector(this, _unpackedCrawl);

            RefreshCurrentView();
            Repaint();
        }
    }
}
