using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    public class ProfilerConnector : MonoBehaviour
    {
        private string connectIP;
        public string IP
        {
            set { connectIP = value; }
        }
        private MemoryProfilerWindow hostWindow;
        public MemoryProfilerWindow HostWindow
        {
            set { hostWindow = value; }
        }

        IEnumerator ConnectIP()
        {
            yield return null;
            hostWindow.connectIP(connectIP);
        }

        IEnumerator ConnectNative()
        {
            yield return new WaitForSeconds(2f);
            hostWindow.connectEditor();
        }

    }

    public class MemoryProfilerWindow : EditorWindow
    {
        public static int Invalid_Int = -1;

        [NonSerialized]
        CrawledMemorySnapshot _unpackedCrawl;
        CrawledMemorySnapshot _preUnpackedCrawl;

        [NonSerialized]
        private bool _registered = false;

        Inspector _inspector;
        TreeMapView _treeMapView;
        MemTableBrowser _tableBrowser;

        ThingInMemory _selectedThing;

        public const int PLAYER_DIRECT_IP_CONNECT_GUID = 65261;

        eShowType m_selectedView = 0;

        static List<string> _SnapshotOptions = new List<string>();

        static List<MemSnapshotInfo> _nativeSnapshotSessions = new List<MemSnapshotInfo>();
        static List<MemSnapshotInfo> _remoteSnapshotSessions = new List<MemSnapshotInfo>();
        static List<MemSnapshotInfo> _loadSnapshotSessions = new List<MemSnapshotInfo>();


        static int _selectedSnapshot = Invalid_Int;

        static string[] _ConnectedOptions = new string[] { "Editor", "Remote", "Load" };

        SnapshotIOperator _snapshotIOperator = new SnapshotIOperator();

        bool _isRemoteConnected = false;

        eProfilerMode _selectedProfilerMode = eProfilerMode.Editor;

        const string ipDefaultTextField = "<ip>";

        [SerializeField]
        string lastLoginIP = ipDefaultTextField;

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

            var savedProfilerIP = EditorPrefs.GetString("ConnectIP");
            if (!string.IsNullOrEmpty(savedProfilerIP))
            {
                lastLoginIP = savedProfilerIP;
            }

            if (PANetDrv.Instance == null)
            {
                PANetDrv.Instance = new PANetDrv();
            }

            clearSnapshotSessions();
            if (NetManager.Instance!=null)
                NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_QueryStacksResponse, Handle_QueryStacksResponse);
        }

        void disConnect()
        {
            ProfilerDriver.connectedProfiler = -1;
            NetManager.Instance.Disconnect();
            _isRemoteConnected = false;
        }

        private void handleCommandEvent()
        {
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName.Equals("AppStarted"))
            {
                GameObject connectObj = new GameObject();
                DontDestroyOnLoad(connectObj);
                connectObj.AddComponent<ProfilerConnector>();
                var pc = connectObj.GetComponent("ProfilerConnector") as ProfilerConnector;
                pc.HostWindow = this;
                pc.StartCoroutine("ConnectNative");
            }

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName.Equals("AppStoped"))
            {
                disConnect();
            }
        }

        public static bool isValidateIPAddress(string ipAddress)
        {
            Regex validipregex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return (ipAddress != "" && validipregex.IsMatch(ipAddress.Trim())) ? true : false;
        }


        public void connectIP(string ip)
        {
            if (!isValidateIPAddress(ip))
            {
                ShowNotification(new GUIContent(string.Format("Invaild IP = {0}!", ip)));
                return;
            }

            if (!NetManager.Instance.Connect(ip))
            {
                ShowNotification(new GUIContent("Connecting failed!)"));
                Debug.LogErrorFormat("Connection failed ip:{0}", ip);
                _isRemoteConnected = false;
                return;
            }

            bool isNative = ip.Equals("127.0.0.1");
            if (!isNative)
            {
                ProfilerDriver.DirectIPConnect(ip);
            }

            if (ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID || isNative)
            {
                var content = new GUIContent(string.Format("Connecting {0} Succeeded!", ip));
                ShowNotification(content);
                _isRemoteConnected = true;
            }
            else
            {
                ShowNotification(new GUIContent("Connecting failed!)"));
                Debug.LogErrorFormat("Connection failed ip:{0}", ip);
                _isRemoteConnected = false;
                if (NetManager.Instance.IsConnected)
                    NetManager.Instance.Disconnect();
                return;
            }
        }


        public void connectEditor()
        {
            ProfilerDriver.connectedProfiler = -1;
            if (NetManager.Instance.IsConnected)
                NetManager.Instance.Disconnect();
            connectIP("127.0.0.1");
            //var content = new GUIContent(string.Format("Connecting Editor Succeeded!"));
            //ShowNotification(content);
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

            if (PANetDrv.Instance != null)
            {
                PANetDrv.Instance.Dispose();
                PANetDrv.Instance = null;
            }
        }

        void freshCurrentSnapshotOptions() 
        {
            var snapshotSession = getCurrentSnapshotSessionList();
            _SnapshotOptions.Clear();
            for (int i = 0; i < snapshotSession.Count;i++)
            {
                _SnapshotOptions.Add(i.ToString());
            }
        }

        void OnSnapshotReceived(PackedMemorySnapshot snapshot)
        {
            var snapshotInfo = new MemSnapshotInfo();
            snapshotInfo.setSnapShotTime(Time.realtimeSinceStartup);

            var snapshotSession = getCurrentSnapshotSessionList();

            snapshotSession.Add(snapshotInfo);
            freshCurrentSnapshotOptions();

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

        public void clearSnapshotSessions()
        {
            _SnapshotOptions.Clear();
            var currentSnapshotSessions = getCurrentSnapshotSessionList();
            if (currentSnapshotSessions!=null)
                currentSnapshotSessions.Clear();
            _tableBrowser.clearTableData();
            _snapshotIOperator.refreshRecordTime();
            ProfilerDriver.connectedProfiler = -1;
            if (NetManager.Instance.IsConnected)
                NetManager.Instance.Disconnect();
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
                        var currentStr = GUILayout.TextField(lastLoginIP, GUILayout.Width(80));
                        if (!lastLoginIP.Equals(currentStr))
                        {
                            lastLoginIP = currentStr;
                            _isRemoteConnected = false;
                        }

                        if (GUI.GetNameOfFocusedControl().Equals("LoginIPTextField") && lastLoginIP.Equals(ipDefaultTextField))
                        {
                            lastLoginIP = "";
                        }

                        bool savedState = GUI.enabled;
                        if (_isRemoteConnected && ProfilerDriver.connectedProfiler == PLAYER_DIRECT_IP_CONNECT_GUID)
                        {
                            GUI.enabled = false;
                        }

                        if (GUILayout.Button("Connect", GUILayout.Width(60)))
                        {
                            GameObject connectObj = new GameObject();
                            connectObj.AddComponent<ProfilerConnector>();
                            var pc = connectObj.GetComponent("ProfilerConnector") as ProfilerConnector;
                            pc.IP = lastLoginIP;
                            pc.HostWindow = this;
                            pc.StartCoroutine("ConnectIP");
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
            if (currentIndex != Invalid_Int && currentIndex != _selectedSnapshot)
            {
                _selectedSnapshot = currentIndex;
                showSnapshotInfo();
            }
        }

        private void takeSnapshotBtn()
        {
            bool savedState = GUI.enabled;
            if (_selectedProfilerMode == eProfilerMode.Remote && ProfilerDriver.connectedProfiler != PLAYER_DIRECT_IP_CONNECT_GUID)
            {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Take Snapshot", GUILayout.Width(100)))
            {
                UnityEditor.MemoryProfiler.MemorySnapshot.RequestNewSnapshot();
            }
            GUI.enabled = savedState;
        }


        void OnGUI()
        {
            handleCommandEvent();

            // main bar
            GUILayout.BeginHorizontal();
            int connectedIndex = GUI.SelectionGrid(new Rect(0, 0, 180, 20), (int)_selectedProfilerMode, _ConnectedOptions, _ConnectedOptions.Length, MemStyles.ToolbarButton);
            if (connectedIndex != (int)_selectedProfilerMode)
            {
                _isRemoteConnected = false;

                _selectedProfilerMode = (eProfilerMode)connectedIndex;

                if (_selectedProfilerMode == (int)eProfilerMode.Editor)
                    connectEditor();

                freshCurrentSnapshotOptions();
            }

            GUILayout.Space(200);
            drawProfilerModeGUI();

            if (GUILayout.Button("Clear Snapshots", GUILayout.MaxWidth(100)))
            {
                if (EditorUtility.DisplayDialog("Clear Sessions", "Warning ! \n\nClear Sessions Will Lost Current Snapshots.", "Continue", "Cancel")) 
                    clearSnapshotSessions();
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

        private void saveSessionBtn()
        {
            _autoSaveToggle = GUILayout.Toggle(_autoSaveToggle, new GUIContent("AutoSave"), GUILayout.MaxWidth(80));
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
                    bool saveSessionSuc = _snapshotIOperator.saveSnapshotSessions(packed, _selectedSnapshot, _selectedProfilerMode, lastLoginIP);
                    MemUtil.LoadSnapshotProgress(0.7f, "save session successed");
                    bool saveJsonSuc =_snapshotIOperator.saveSnapshotJsonFile(currentSession.unPacked, _selectedSnapshot, _selectedProfilerMode, lastLoginIP);
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
                case eProfilerMode.File:
                    {
                        return _loadSnapshotSessions;
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

            NetManager.Instance.RegisterCmdHandler(eNetCmd.SV_QueryStacksResponse, Handle_QueryStacksResponse);
            RefreshCurrentView();
            Repaint();
        }
    }
}
