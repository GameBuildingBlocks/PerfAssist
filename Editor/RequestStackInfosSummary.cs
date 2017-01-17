using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

public class RequestStackInfosSummary
{
    private class stackParamater
    {
        int instanceID;
        public int InstanceID
        {
            get { return instanceID; }
            set { instanceID = value; }
        }
        int size;
        public int Size
        {
            get { return size; }
            set { size = value; }
        }
    }

#if UNITY_EDITOR
    [MenuItem(PAEditorConst.DevCommandPath + "/RequestStackInfosSummary")]
    static void RequestStackSummary()
    {
#if UNITY_EDITOR
        if (NetManager.Instance == null)
            return;
        MemoryProfilerWindow.MemoryProfilerWindow w = EditorWindow.GetWindow<MemoryProfilerWindow.MemoryProfilerWindow>("MemoryProfilerWindow");
        if (w.GetType().Name == "MemoryProfilerWindow")
        {
            if (w.UnpackedCrawl == null)
                return;

            Dictionary<string, List<stackParamater>> categoryDict = new Dictionary<string, List<stackParamater>>();

            foreach (var trackCategory in SceneGraphExtractor.MemCategories)
            {
                if (!categoryDict.ContainsKey(trackCategory))
                {
                    categoryDict.Add(trackCategory, new List<stackParamater>());
                }
            }

            foreach (var obj in w.UnpackedCrawl.nativeObjects)
            {
                if (categoryDict.ContainsKey(obj.className))
                {
                    List<stackParamater> list;
                    categoryDict.TryGetValue(obj.className, out list);
                    var info = new stackParamater();
                    info.InstanceID = obj.instanceID;
                    info.Size = obj.size;
                    list.Add(info);
                }
            }

            UsCmd cmd = new UsCmd();
            cmd.WriteInt16((short)eNetCmd.CL_RequestStackSummary);
            cmd.WriteString("begin");
            NetManager.Instance.Send(cmd);

            int passCountPerCmd = 500;
            foreach (var categoryPair in categoryDict)
            {
                int count = categoryPair.Value.Count;
                int times = count / passCountPerCmd;
                int residue = count % passCountPerCmd;

                for (int i = 0; i < times; i++)
                {
                    cmd = new UsCmd();
                    cmd.WriteInt16((short)eNetCmd.CL_RequestStackSummary);
                    cmd.WriteString(categoryPair.Key);
                    cmd.WriteInt32(passCountPerCmd);
                    for (int j = i * passCountPerCmd; j < (i + 1) * passCountPerCmd;j++)
                    {
                        var info =categoryPair.Value[j];
                        cmd.WriteInt32(info.InstanceID);
                        cmd.WriteInt32(info.Size);
                    }
                    NetManager.Instance.Send(cmd);
                }

                if (residue > 0)
                {
                    cmd = new UsCmd();
                    cmd.WriteInt16((short)eNetCmd.CL_RequestStackSummary);
                    cmd.WriteString(categoryPair.Key);
                    cmd.WriteInt32(residue);
                    for (int i = 0; i <residue;i++)
                    {
                        var info = categoryPair.Value[times * passCountPerCmd + i];
                        cmd.WriteInt32(info.InstanceID);
                        cmd.WriteInt32(info.Size);
                    }
                    NetManager.Instance.Send(cmd);
                }
            }

            cmd = new UsCmd();
            cmd.WriteInt16((short)eNetCmd.CL_RequestStackSummary);
            cmd.WriteString("end");
            cmd.WriteInt32(categoryDict.Count);
            foreach (var categoryPair in categoryDict)
            {
                cmd.WriteString(categoryPair.Key);
                cmd.WriteInt32(categoryPair.Value.Count);
                int categoryTotalSize=0;
                foreach (var info in categoryPair.Value)
                {
                    categoryTotalSize += info.Size;
                }
                cmd.WriteInt32(categoryTotalSize);
            }
            NetManager.Instance.Send(cmd);
        }
#endif
    }
#endif
}