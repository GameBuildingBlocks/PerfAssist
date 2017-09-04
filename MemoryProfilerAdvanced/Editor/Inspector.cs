using System;
using UnityEditor;
using UnityEngine;
using UnityEditor.MemoryProfiler;
using System.Linq;
using System.Collections.Generic;

namespace MemoryProfilerWindow
{
    public class Inspector
    {
        public ThingInMemory Selected { get { return _selectedThing; } }

        ThingInMemory _selectedThing;
        private ThingInMemory[] _shortestPath;
        private ShortestPathToRootFinder _shortestPathToRootFinder;
        Vector2 _scrollPosition;
        MemoryProfilerWindow _hostWindow;
        CrawledMemorySnapshot _unpackedCrawl;
        PrimitiveValueReader _primitiveValueReader;
        Dictionary<ulong, ThingInMemory> objectCache = new Dictionary<ulong, ThingInMemory>();
        private Texture2D _textureObject;
        private int _prevInstance;
        private float _textureSize = 128.0f;

        MemObjHistory Instance = new MemObjHistory();

        static class Styles
        {
            public static GUIStyle entryEven = "OL EntryBackEven";
            public static GUIStyle entryOdd = "OL EntryBackOdd";
        }

        GUILayoutOption labelWidth = GUILayout.Width(150);

        private Texture2D _textureBack;
        private Texture2D _textureForward;

        public string _stackInfo = "";

        public Inspector(MemoryProfilerWindow hostWindow, CrawledMemorySnapshot unpackedCrawl)
        {
            _unpackedCrawl = unpackedCrawl;
            _hostWindow = hostWindow;
            _shortestPathToRootFinder = new ShortestPathToRootFinder(unpackedCrawl);
            _primitiveValueReader = new PrimitiveValueReader(_unpackedCrawl.virtualMachineInformation, _unpackedCrawl.managedHeap);

            _textureBack = Resources.Load("back") as Texture2D;
            _textureForward = Resources.Load("forward") as Texture2D;
        }

        public void SelectThing(ThingInMemory thing)
        {
            _selectedThing = thing;
            _shortestPath = _shortestPathToRootFinder.FindFor(thing);
            Instance.OnObjSelected(thing);

            //if (NetManager.Instance != null && NetManager.Instance.IsConnected)
            //    requestStackDataInfo();
        }

        public void Draw()
        {
            float NavAreaHeight = 40.0f;
            GUILayout.BeginArea(new Rect(_hostWindow.position.width - MemConst.InspectorWidth,
                MemConst.TopBarHeight, MemConst.InspectorWidth, NavAreaHeight));

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Space(3);

            GUI.enabled = Instance.TryGetPrev() != null;
            if (GUILayout.Button(new GUIContent("Back", _textureBack), GUILayout.MinWidth(100), GUILayout.MaxHeight(25)))
            {
                ThingInMemory prev = Instance.MovePrev();
                if (prev != null)
                    _hostWindow.SelectThing(prev);
            }
            GUI.enabled = Instance.TryGetNext() != null;
            if (GUILayout.Button(new GUIContent("Forward", _textureForward), GUILayout.MinWidth(100), GUILayout.MaxHeight(25)))
            {
                ThingInMemory next = Instance.MoveNext();
                if (next != null)
                    _hostWindow.SelectThing(next);
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            float topSpace = MemConst.TopBarHeight + NavAreaHeight;
            GUILayout.BeginArea(new Rect(_hostWindow.position.width - MemConst.InspectorWidth, topSpace, MemConst.InspectorWidth, _hostWindow.position.height - topSpace));
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_selectedThing == null)
                GUILayout.Label("Select an object to see more info");
            else
            {
                var nativeObject = _selectedThing as NativeUnityEngineObject;
                if (nativeObject != null)
                {
                    GUILayout.Label("NativeUnityEngineObject", EditorStyles.boldLabel);
                    GUILayout.Space(5);
                    EditorGUILayout.LabelField("Name", nativeObject.name);
                    EditorGUILayout.LabelField("ClassName", nativeObject.className);
                    EditorGUILayout.LabelField("ClassID", nativeObject.classID.ToString());
                    EditorGUILayout.LabelField("instanceID", nativeObject.instanceID.ToString());
                    EditorGUILayout.LabelField("isDontDestroyOnLoad", nativeObject.isDontDestroyOnLoad.ToString());
                    EditorGUILayout.LabelField("isPersistent", nativeObject.isPersistent.ToString());
                    EditorGUILayout.LabelField("isManager", nativeObject.isManager.ToString());
                    EditorGUILayout.LabelField("hideFlags", nativeObject.hideFlags.ToString());
                    EditorGUILayout.LabelField("hideFlags", nativeObject.size.ToString());
                    DrawSpecificTexture2D(nativeObject);
                }

                var managedObject = _selectedThing as ManagedObject;
                if (managedObject != null)
                {
                    GUILayout.Label("ManagedObject");
                    EditorGUILayout.LabelField("Type", managedObject.typeDescription.name);
                    EditorGUILayout.LabelField("Address", managedObject.address.ToString("X"));
                    EditorGUILayout.LabelField("size", managedObject.size.ToString());

                    if (managedObject.typeDescription.name == "System.String")
                        EditorGUILayout.LabelField("value", StringTools.ReadString(_unpackedCrawl.managedHeap.Find(managedObject.address, _unpackedCrawl.virtualMachineInformation), _unpackedCrawl.virtualMachineInformation));
                    DrawFields(managedObject);

                    if (managedObject.typeDescription.isArray)
                    {
                        DrawArray(managedObject);
                    }
                }

                if (_selectedThing is GCHandle)
                {
                    GUILayout.Label("GCHandle");
                    EditorGUILayout.LabelField("size", _selectedThing.size.ToString());
                }

                var staticFields = _selectedThing as StaticFields;
                if (staticFields != null)
                {
                    GUILayout.Label("Static Fields");
                    GUILayout.Label("Of type: " + staticFields.typeDescription.name);
                    GUILayout.Label("size: " + staticFields.size);

                    DrawFields(staticFields.typeDescription, new BytesAndOffset() { bytes = staticFields.typeDescription.staticFieldBytes, offset = 0, pointerSize = _unpackedCrawl.virtualMachineInformation.pointerSize }, true);
                }

                if (managedObject == null)
                {
                    GUILayout.Space(10);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("References:", labelWidth);
                    GUILayout.BeginVertical();
                    DrawLinks(_selectedThing.references);
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(10);
                GUILayout.Label("Referenced by:");
                DrawLinks(_selectedThing.referencedBy);

                GUILayout.Space(10);
                if (_shortestPath != null)
                {
                    if (_shortestPath.Length > 1)
                    {
                        GUILayout.Label("ShortestPathToRoot");
                        DrawLinks(_shortestPath);
                    }
                    string reason;
                    _shortestPathToRootFinder.IsRoot(_shortestPath.Last(), out reason);
                    GUILayout.Label("This is a root because:");
                    GUILayout.TextArea(reason);
                }
                else
                {
                    GUILayout.TextArea("No root is keeping this object alive. It will be collected next UnloadUnusedAssets() or scene load");
                }
            }

            //if (NetManager.Instance == null || !NetManager.Instance.IsConnected)
            //    _stackInfo = GetNativeDebugInfo();
            //GUILayout.TextArea(_stackInfo, GUILayout.MinHeight(300f));

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawSpecificTexture2D(NativeUnityEngineObject nativeObject)
        {
            if (nativeObject.className != "Texture2D")
            {
                _textureObject = null;
                return;
            }
            EditorGUILayout.HelpBox("Watching Texture Detail Data is only for Editor.", MessageType.Warning, true);
            if (_prevInstance != nativeObject.instanceID)
            {
                _textureObject = EditorUtility.InstanceIDToObject(nativeObject.instanceID) as Texture2D;
                _prevInstance = nativeObject.instanceID;
            }
            if (_textureObject != null)
            {
                EditorGUILayout.LabelField("textureInfo: " + _textureObject.width + "x" + _textureObject.height + " " + _textureObject.format);
                EditorGUILayout.ObjectField(_textureObject, typeof(Texture2D), false);
                _textureSize = EditorGUILayout.Slider(_textureSize, 100.0f, 1024.0f);
                GUILayout.Label(_textureObject, GUILayout.Width(_textureSize), GUILayout.Height(_textureSize * _textureObject.height / _textureObject.width));
            }
            else
            {
                EditorGUILayout.LabelField("Can't instance texture,maybe it was already released.");
            }
        }

        private void DrawArray(ManagedObject managedObject)
        {
            var typeDescription = managedObject.typeDescription;
            int elementCount = ArrayTools.ReadArrayLength(_unpackedCrawl.managedHeap, managedObject.address, typeDescription, _unpackedCrawl.virtualMachineInformation);
            GUILayout.Label("element count: " + elementCount);
            int rank = typeDescription.arrayRank;
            GUILayout.Label("arrayRank: " + rank);
            if (_unpackedCrawl.typeDescriptions[typeDescription.baseOrElementTypeIndex].isValueType)
            {
                GUILayout.Label("Cannot yet display elements of value type arrays");
                return;
            }
            if (rank != 1)
            {
                GUILayout.Label("Cannot display non rank=1 arrays yet.");
                return;
            }

            var pointers = new List<UInt64>();
            for (int i = 0; i != elementCount; i++)
            {
                pointers.Add(_primitiveValueReader.ReadPointer(managedObject.address + (UInt64)_unpackedCrawl.virtualMachineInformation.arrayHeaderSize + (UInt64)(i * _unpackedCrawl.virtualMachineInformation.pointerSize)));
            }
            GUILayout.Label("elements:");
            DrawLinks(pointers);
        }

        private void DrawFields(TypeDescription typeDescription, BytesAndOffset bytesAndOffset, bool useStatics = false)
        {
            int counter = 0;
            foreach (var field in TypeTools.AllFieldsOf(typeDescription, _unpackedCrawl.typeDescriptions, useStatics ? TypeTools.FieldFindOptions.OnlyStatic : TypeTools.FieldFindOptions.OnlyInstance))
            {
                counter++;
                var gUIStyle = counter % 2 == 0 ? Styles.entryEven : Styles.entryOdd;
                gUIStyle.margin = new RectOffset(0, 0, 0, 0);
                gUIStyle.overflow = new RectOffset(0, 0, 0, 0);
                gUIStyle.padding = EditorStyles.label.padding;
                GUILayout.BeginHorizontal(gUIStyle);
                GUILayout.Label(field.name, labelWidth);
                GUILayout.BeginVertical();
                DrawValueFor(field, bytesAndOffset.Add(field.offset));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFields(ManagedObject managedObject)
        {
            if (managedObject.typeDescription.isArray)
                return;
            GUILayout.Space(10);
            GUILayout.Label("Fields:");
            DrawFields(managedObject.typeDescription, _unpackedCrawl.managedHeap.Find(managedObject.address, _unpackedCrawl.virtualMachineInformation));
        }

        private void DrawValueFor(FieldDescription field, BytesAndOffset bytesAndOffset)
        {
            var typeDescription = _unpackedCrawl.typeDescriptions[field.typeIndex];

            try
            {
                switch (typeDescription.name)
                {
                case "System.Int32":
                    GUILayout.Label(_primitiveValueReader.ReadInt32(bytesAndOffset).ToString());
                    break;
                case "System.Int64":
                    GUILayout.Label(_primitiveValueReader.ReadInt64(bytesAndOffset).ToString());
                    break;
                case "System.UInt32":
                    GUILayout.Label(_primitiveValueReader.ReadUInt32(bytesAndOffset).ToString());
                    break;
                case "System.UInt64":
                    GUILayout.Label(_primitiveValueReader.ReadUInt64(bytesAndOffset).ToString());
                    break;
                case "System.Int16":
                    GUILayout.Label(_primitiveValueReader.ReadInt16(bytesAndOffset).ToString());
                    break;
                case "System.UInt16":
                    GUILayout.Label(_primitiveValueReader.ReadUInt16(bytesAndOffset).ToString());
                    break;
                case "System.Byte":
                    GUILayout.Label(_primitiveValueReader.ReadByte(bytesAndOffset).ToString());
                    break;
                case "System.SByte":
                    GUILayout.Label(_primitiveValueReader.ReadSByte(bytesAndOffset).ToString());
                    break;
                case "System.Char":
                    GUILayout.Label(_primitiveValueReader.ReadChar(bytesAndOffset).ToString());
                    break;
                case "System.Boolean":
                    GUILayout.Label(_primitiveValueReader.ReadBool(bytesAndOffset).ToString());
                    break;
                case "System.Single":
                    GUILayout.Label(_primitiveValueReader.ReadSingle(bytesAndOffset).ToString());
                    break;
                case "System.Double":
                    GUILayout.Label(_primitiveValueReader.ReadDouble(bytesAndOffset).ToString());
                    break;
                case "System.IntPtr":
                    GUILayout.Label(_primitiveValueReader.ReadPointer(bytesAndOffset).ToString("X"));
                    break;
                default:
                    if (!typeDescription.isValueType)
                    {
                        ThingInMemory item = GetThingAt(bytesAndOffset.ReadPointer());
                        if (item == null)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Button("Null");
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            DrawLinks(new ThingInMemory[] { item });
                        }
                    }
                    else
                    {
                        DrawFields(typeDescription, bytesAndOffset);
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                GUILayout.Label(string.Format("<bad_entry> type: {0}, len: {1}, offset: {2}, ex: {3}", typeDescription.name, bytesAndOffset.bytes.Length, bytesAndOffset.offset, ex.GetType().Name));
                Debug.LogFormat("<bad_entry> type: {0}, len: {1}, offset: {2}, ex: {3}", typeDescription.name, bytesAndOffset.bytes.Length, bytesAndOffset.offset, ex.GetType().Name);
            }
        }

        private ThingInMemory GetThingAt(ulong address)
        {
            if (!objectCache.ContainsKey(address))
            {
                objectCache[address] = _unpackedCrawl.allObjects.OfType<ManagedObject>().FirstOrDefault(mo => mo.address == address);
            }

            return objectCache[address];
        }

        /*
        private Item FindItemPointedToByManagedFieldAt(BytesAndOffset bytesAndOffset)
        {
            var stringAddress = _primitiveValueReader.ReadPointer(bytesAndOffset);
            return
                _items.FirstOrDefault(i =>
                    {
                        var m = i._thingInMemory as ManagedObject;
                        if (m != null)
                        {
                            return m.address == stringAddress;
                        }
                        return false;
                    });
        }*/

        private void DrawLinks(IEnumerable<UInt64> pointers)
        {
            DrawLinks(pointers.Select(p => GetThingAt(p)));
        }

        private void DrawLinks(IEnumerable<ThingInMemory> thingInMemories)
        {
            var c = GUI.backgroundColor;
            GUI.skin.button.alignment = TextAnchor.UpperLeft;
            foreach (var rb in thingInMemories)
            {
                EditorGUI.BeginDisabledGroup(rb == _selectedThing || rb == null);

                GUI.backgroundColor = ColorFor(rb);

                var caption = rb == null ? "null" : rb.caption;

                var managedObject = rb as ManagedObject;
                if (managedObject != null && managedObject.typeDescription.name == "System.String")
                    caption = StringTools.ReadString(_unpackedCrawl.managedHeap.Find(managedObject.address, _unpackedCrawl.virtualMachineInformation), _unpackedCrawl.virtualMachineInformation);

                if (GUILayout.Button(caption))
                    _hostWindow.SelectThing(rb);
                EditorGUI.EndDisabledGroup();
            }
            GUI.backgroundColor = c;
        }

        private Color ColorFor(ThingInMemory rb)
        {
            if (rb == null)
                return Color.gray;
            if (rb is NativeUnityEngineObject)
                return Color.red;
            if (rb is ManagedObject)
                return Color.Lerp(Color.blue, Color.white, 0.5f);
            if (rb is GCHandle)
                return Color.magenta;
            if (rb is StaticFields)
                return Color.yellow;

            throw new ArgumentException("Unexpected type: " + rb.GetType());
        }

        private string GetNativeDebugInfo()
        {
            var obj = _selectedThing as NativeUnityEngineObject;
            if (obj == null || ResourceTracker.Instance == null || !ResourceTracker.Instance.EnableTracking)
                return "";

            ResourceRequestInfo requestInfo = ResourceTracker.Instance.GetAllocInfo(obj.instanceID);
            if (requestInfo == null)
                return "";

            return string.Format("{0}\n\nStackTrace:\n{1}", requestInfo.ToString(), ResourceTracker.Instance.GetStackTrace(requestInfo));
        }

        private void requestStackDataInfo()
        {
            var selectNat = _selectedThing as NativeUnityEngineObject;
            if (selectNat == null)
                return;
            UsCmd cmd = new UsCmd();
            cmd.WriteInt16((short)eNetCmd.CL_RequestStackData);
            cmd.WriteInt32(selectNat.instanceID);
            cmd.WriteString(getRemoveDiffTypeStr(selectNat));
            NetManager.Instance.Send(cmd);
        }

        private string getRemoveDiffTypeStr(NativeUnityEngineObject things)
        {
            return things.className.Replace(sDiffType.AdditiveType, "").Replace(sDiffType.ModificationType, "").
                Replace(sDiffType.NegativeType, "");
        }
    }
}
