using System;
using System.Collections.Generic;
using System.IO;
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

namespace MemoryProfilerWindow
{
    public class MemoryProfilerWindow : EditorWindow
    {
        public CrawledMemorySnapshot UnpackedCrawl { get { return _unpackedCrawl; } }
        CrawledMemorySnapshot _unpackedCrawl;
        CrawledMemorySnapshot _preUnpackedCrawl;

        Inspector _inspector;
        TreeMapView _treeMapView;
        MemTableBrowser _tableBrowser;

        ThingInMemory _selectedThing;
        eShowType m_selectedView = 0;

        StackInfoSynObj _stackInfoObj = new StackInfoSynObj();
        TrackerModeManager _modeMgr = new TrackerModeManager();

        [MenuItem(PAEditorConst.MenuPath + "/ResourceTracker")]
        static void Create()
        {
            EditorWindow.GetWindow<MemoryProfilerWindow>();
        }

        MemoryProfilerWindow()
        {
            MemorySnapshot.OnSnapshotReceived += OnSnapshotReceived;
            _modeMgr.SetSelectionChanged(OnSnapshotSelectionChanged);
        }

        void InitNet()
        {
            if (NetManager.Instance == null)
            {
                NetUtil.LogHandler = Debug.LogFormat;
                NetUtil.LogErrorHandler = Debug.LogErrorFormat;

                NetManager.Instance = new NetManager();
                NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_App_Logging, TrackerModeUtil.Handle_ServerLogging);
                NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_QueryStacksResponse, Handle_QueryStacksResponse);
            }
        }

        void Awake()
        {
            InitNet();
        }

        void OnEnable()
        {
            if (_treeMapView == null)
                _treeMapView = new TreeMapView();

            if (_tableBrowser == null)
                _tableBrowser = new MemTableBrowser(this);
        }

        void OnDestroy()
        {
            if (_treeMapView != null)
                _treeMapView.CleanupMeshes();

            if (NetManager.Instance != null)
            {
                NetManager.Instance.Dispose();
                NetManager.Instance = null;
            }
        }

        void OnSnapshotReceived(PackedMemorySnapshot packed)
        {
            TrackerMode_Base curMode = _modeMgr.GetCurrentMode();
            if (curMode != null)
            {
                var snapshotInfo = new MemSnapshotInfo();
                snapshotInfo.setSnapShotTime(Time.realtimeSinceStartup);

                MemUtil.LoadSnapshotProgress(0.01f, "creating Crawler");
                var packedCrawled = new Crawler().Crawl(packed);
                MemUtil.LoadSnapshotProgress(0.7f, "unpacking");
                snapshotInfo.unPacked = CrawlDataUnpacker.Unpack(packedCrawled);
                MemUtil.LoadSnapshotProgress(1.0f, "done");

                curMode.AddSnapshot(snapshotInfo);

                if (!curMode.SaveSessionInfo(packed, snapshotInfo.unPacked))
                    Debug.LogErrorFormat("Save Session Info Failed!");
            }
        }

        void Update()
        {
            _modeMgr.Update();

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

        void OnGUI()
        {
            try
            {
                if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName.Equals("AppStarted"))
                {
                    InitNet();

                    var curMode = _modeMgr.GetCurrentMode();
                    if (curMode != null)
                        curMode.OnAppStarted();
                }

                _modeMgr.OnGUI();

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

        private bool Handle_QueryStacksResponse(eNetCmd cmd, UsCmd c)
        {
            var stackInfo = c.ReadString();
            if (string.IsNullOrEmpty(stackInfo))
                return false;

            _stackInfoObj.writeStackInfo(stackInfo);
            NetUtil.Log("stack info{0}", stackInfo);
            return true;
        }

        public void RefreshCurrentView()
        {
            if (_unpackedCrawl == null)
                return;

            switch (m_selectedView)
            {
                case eShowType.InTable:
                    if (_tableBrowser != null)
                        if (_tableBrowser._showdiffToggle && _preUnpackedCrawl != null)
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
