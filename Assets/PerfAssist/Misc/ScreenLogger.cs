using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

namespace AClockworkBerry
{
    public class TopNContainer
    {
        public const int MaxCount = 6;

        public List<KeyValuePair<double, string>> TopN = new List<KeyValuePair<double, string>>(MaxCount);

        public bool TryAdd(double value, string text)
        {
            if (TopN.Count == MaxCount && value <= TopN[TopN.Count - 1].Key)
            {
                return false;
            }

            bool inserted = false;
            for (int i = 0; i < TopN.Count; ++i)
            {
                if (value > TopN[i].Key)
                {
                    TopN.Insert(i, new KeyValuePair<double, string>(value, text));
                    inserted = true;
                    break;
                }
            }

            if (!inserted)
            {
                if (TopN.Count < MaxCount)
                {
                    TopN.Add(new KeyValuePair<double, string>(value, text));
                    return true;
                }
                else
                {
                    Debug.LogWarningFormat("[TopNContainer] (大于下限却找不到插入点)。(len: {0}, lowest: {1}, value: {2})",
                        TopN.Count, TopN[TopN.Count - 1].Key, value);
                    return false;
                }
            }
            else
            {
                while (TopN.Count > MaxCount)
                {
                    TopN.RemoveAt(TopN.Count - 1);
                }
            }

            return true;
        }

        private StringBuilder m_strBuilder = new StringBuilder(256);

        public string ItemToString(int i)
        {
            m_strBuilder.Length = 0;
            return m_strBuilder.AppendFormat("({0:0.00}) {1}", TopN[i].Key, TopN[i].Value).ToString();
        }

        public void Clear()
        {
            TopN.Clear();
        }
    }


    public class ScreenLogger : MonoBehaviour
    {
        public static bool IsPersistent = true;
        public static bool Instantiated { get { return instantiated; } }

        private static ScreenLogger instance;
        private static bool instantiated = false;

        class LogMessage
        {
            public string Message;
            public LogType Type;

            public LogMessage(string msg, LogType type)
            {
                Message = msg;
                Type = type;
            }
        }

        public enum LogAnchor
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        public bool ShowLog = true;
        public bool ShowInEditor = true;


        [Tooltip("Height of the log area as a percentage of the screen height")]
        [Range(0.3f, 1.0f)]
        public float Height = 0.7f;

        [Tooltip("Width of the log area as a percentage of the screen width")]
        [Range(0.3f, 1.0f)]
        public float Width = 0.7f;

        public int Margin = 20;

        public LogAnchor AnchorPosition = LogAnchor.BottomLeft;

        public int FontSize = 16;

        [Range(0f, 01f)]
        public float BackgroundOpacity = 0.5f;
        public Color BackgroundColor = Color.black;

        public bool LogMessages = true;
        public bool LogWarnings = true;
        public bool LogErrors = true;

        public Color MessageColor = Color.white;
        public Color WarningColor = Color.yellow;
        public Color ErrorColor = new Color(1, 0.5f, 0.5f);

        public Dictionary<string, Color> TagColors = new Dictionary<string, Color>()
        {
            { "#LuaCache", Color.green },
            { "#LuaIO", Color.red },
        };

        public bool StackTraceMessages = false;
        public bool StackTraceWarnings = false;
        public bool StackTraceErrors = true;

        static Queue<LogMessage> queue = new Queue<LogMessage>();

        GUIStyle styleContainer, styleText;
        int padding = 20;

        private bool destroying = false;

        public static ScreenLogger Instance
        {
            get
            {
                if (instantiated) return instance;

                instance = GameObject.FindObjectOfType(typeof(ScreenLogger)) as ScreenLogger;

                // Object not found, we create a new one
                if (instance == null)
                {
                    //// Try to load the default prefab
                    //try
                    //{
                    //    instance = Instantiate(Resources.Load("ScreenLoggerPrefab", typeof(ScreenLogger))) as ScreenLogger;
                    //}
                    //catch (Exception e)
                    //{
                    //    Debug.Log("Failed to load default Screen Logger prefab...");
                    //}
                    instance = new GameObject("ScreenLogger", typeof(ScreenLogger)).GetComponent<ScreenLogger>();

                    // Problem during the creation, this should not happen
                    if (instance == null)
                    {
                        UnityEngine.Debug.LogError("Problem during the creation of ScreenLogger");
                    }
                    else instantiated = true;
                }
                else
                {
                    instantiated = true;
                }

                return instance;
            }
        }

        public void Awake()
        {
            ScreenLogger[] obj = GameObject.FindObjectsOfType<ScreenLogger>();

            if (obj.Length > 1)
            {
                UnityEngine.Debug.Log("Destroying ScreenLogger, already exists...");
                
                destroying = true;

                Destroy(gameObject);
                return;
            }

            InitStyles();

            if (IsPersistent)
                DontDestroyOnLoad(this);
        }

        private void InitStyles()
        {
            Texture2D back = new Texture2D(1, 1);
            BackgroundColor.a = BackgroundOpacity;
            back.SetPixel(0, 0, BackgroundColor);
            back.Apply();

            styleContainer = new GUIStyle();
            styleContainer.normal.background = back;
            styleContainer.wordWrap = true;
            styleContainer.padding = new RectOffset(padding, padding, padding, padding);

            styleText = new GUIStyle();
            styleText.fontSize = FontSize;
        }

        void OnEnable()
        {
            if (!ShowInEditor && Application.isEditor) return;

            queue = new Queue<LogMessage>();

#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            Application.RegisterLogCallback(HandleLog);
#else
            Application.logMessageReceived += HandleLog;
#endif
        }

        void OnDisable()
        {
            // If destroyed because already exists, don't need to de-register callback
            if (destroying) return;

#if UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            Application.RegisterLogCallback(null);
#else
            Application.logMessageReceived -= HandleLog;
#endif
        }

        void Update()
        {
            if (!ShowInEditor && Application.isEditor) return;

            float InnerHeight = (Screen.height - 2 * Margin) * Height - 2 * padding;
            int TotalRows = (int) (InnerHeight / styleText.lineHeight);

            // Remove overflowing rows
            while (queue.Count > TotalRows)
                queue.Dequeue();
        }


        void OnGUI()
        {
            if (!ShowLog) return;
            if (!ShowInEditor && Application.isEditor) return;

            float w = (Screen.width - 2 * Margin) * Width;
            float h = (Screen.height - 2 * Margin) * Height;
            float x = 1, y = 1;

            switch (AnchorPosition)
            {
                case LogAnchor.BottomLeft:
                    x = Margin;
                    y = Margin + (Screen.height - 2 * Margin) * (1 - Height);
                    break;

                case LogAnchor.BottomRight:
                    x = Margin + (Screen.width - 2 * Margin) * (1 - Width);
                    y = Margin + (Screen.height - 2 * Margin) * (1 - Height);
                    break;

                case LogAnchor.TopLeft:
                    x = Margin;
                    y = Margin;
                    break;

                case LogAnchor.TopRight:
                    x = Margin + (Screen.width - 2 * Margin) * (1 - Width);
                    y = Margin;
                    break;
            }

            GUILayout.BeginArea(new Rect(x, y, w, h), styleContainer);

            foreach (LogMessage m in queue)
            {
                switch (m.Type)
                {
                    case LogType.Warning:
                        styleText.normal.textColor = WarningColor;
                        break;

                    case LogType.Log:
                        styleText.normal.textColor = MessageColor;
                        break;

                    case LogType.Assert:
                    case LogType.Exception:
                    case LogType.Error:
                        styleText.normal.textColor = ErrorColor;
                        break;

                    default:
                        styleText.normal.textColor = MessageColor;
                        break;
                }

                if (m.Message.Contains("#"))
                {
                    foreach (var p in TagColors)
                    {
                        if (m.Message.Contains(p.Key))
                        {
                            styleText.normal.textColor = p.Value;
                            break;
                        }
                    }
                }

                GUILayout.Label(m.Message, styleText);
            }

            GUILayout.EndArea();

            float topNWidth = 550.0f;
            float topNHeight = (Screen.height - 2 * Margin - Margin) * (1.0f - Height);
            styleText.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(Margin, Margin, topNWidth, topNHeight), styleContainer);
            GUILayout.Label("Sync Top N", styleText);
            for (int i = 0; i < SyncTopN.TopN.Count; ++i)
                GUILayout.Label(SyncTopN.ItemToString(i), styleText);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(Margin * 2 + topNWidth, Margin, topNWidth, topNHeight), styleContainer);
            GUILayout.Label("Async Top N", styleText);
            for (int i = 0; i < AsyncTopN.TopN.Count; ++i)
                GUILayout.Label(AsyncTopN.ItemToString(i), styleText);
            GUILayout.EndArea();
        }

        public TopNContainer SyncTopN = new TopNContainer();
        public TopNContainer AsyncTopN = new TopNContainer();

        public void Clear()
        {
            SyncTopN.Clear();
            AsyncTopN.Clear();
        }

        void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Assert && !LogErrors) return;
            if (type == LogType.Error && !LogErrors) return;
            if (type == LogType.Exception && !LogErrors) return;
            if (type == LogType.Log && !LogMessages) return;
            if (type == LogType.Warning && !LogWarnings) return;

            string[] lines = message.Split(new char[] { '\n' });

            foreach (string l in lines)
                queue.Enqueue(new LogMessage(l, type));

            if (type == LogType.Assert && !StackTraceErrors) return;
            if (type == LogType.Error && !StackTraceErrors) return;
            if (type == LogType.Exception && !StackTraceErrors) return;
            if (type == LogType.Log && !StackTraceMessages) return;
            if (type == LogType.Warning && !StackTraceWarnings) return;

            string[] trace = stackTrace.Split(new char[] { '\n' });

            foreach (string t in trace)
                if (t.Length != 0) queue.Enqueue(new LogMessage("  " + t, type));
        }

        //StringBuilder m_strBuilder = new StringBuilder(256);

        //string GetStackFrame(int skipFrames)
        //{
        //    var frame = new StackFrame(skipFrames, true);
        //    if (frame == null || frame.GetMethod() == null)
        //        return ""; 

        //    m_strBuilder.Length = 0;
        //    m_strBuilder.AppendFormat("    {0}() - {1}", frame.GetMethod().Name, frame.GetFileName());
        //    return m_strBuilder.ToString();
        //}

        public void EnqueueDirectly(string message, LogType type)
        {
            queue.Enqueue(new LogMessage(message, type));
            //queue.Enqueue(new LogMessage(GetStackFrame(10), type));
        }

        public void InspectorGUIUpdated()
        {
            InitStyles();
        }
    }
}

/*
The MIT License

Copyright © 2016 Screen Logger - Giuseppe Portelli <giuseppe@aclockworkberry.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/