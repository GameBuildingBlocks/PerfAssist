using System;
using System.Collections.Generic;


namespace CoInternal
{
    class ObjectInfo
    {
        public int instanceId;

        public int memorySize;

        public int reason;

        public List<ObjectInfo> referencedBy;

        public string name;

        public string className;
    }
}

namespace CoInternal
{
    [Serializable]
    class MemoryElement
    {
        public List<MemoryElement> children;

        public MemoryElement parent;

        public ObjectInfo memoryInfo;

        public int totalMemory;

        public int totalChildCount;

        public string name;

        public bool expanded;

        public string description;

        public MemoryElement()
        {
            this.children = new List<MemoryElement>();
        }

        public MemoryElement(string n)
        {
            this.expanded = false;
            this.name = n;
            this.children = new List<MemoryElement>();
            this.description = string.Empty;
        }

        public MemoryElement(ObjectInfo memInfo, bool finalize)
        {
            this.expanded = false;
            this.memoryInfo = memInfo;
            this.name = this.memoryInfo.name;
            this.totalMemory = ((memInfo == null) ? 0 : memInfo.memorySize);
            this.totalChildCount = 1;
            if (finalize)
            {
                this.children = new List<MemoryElement>();
            }
        }

        public MemoryElement(string n, List<MemoryElement> groups)
        {
            this.name = n;
            this.expanded = false;
            this.description = string.Empty;
            this.totalMemory = 0;
            this.totalChildCount = 0;
            this.children = new List<MemoryElement>();
            foreach (MemoryElement current in groups)
            {
                this.AddChild(current);
            }
        }

        public void ExpandChildren()
        {
            if (this.children != null)
            {
                return;
            }
            this.children = new List<MemoryElement>();
            for (int i = 0; i < this.ReferenceCount(); i++)
            {
                this.AddChild(new MemoryElement(this.memoryInfo.referencedBy[i], false));
            }
        }

        public int AccumulatedChildCount()
        {
            return this.totalChildCount;
        }

        public int ChildCount()
        {
            if (this.children != null)
            {
                return this.children.Count;
            }
            return this.ReferenceCount();
        }

        public int ReferenceCount()
        {
            return (this.memoryInfo == null || this.memoryInfo.referencedBy == null) ? 0 : this.memoryInfo.referencedBy.Count;
        }

        public void AddChild(MemoryElement node)
        {
            if (node == this)
            {
                throw new Exception("Should not AddChild to itself");
            }
            this.children.Add(node);
            node.parent = this;
            this.totalMemory += node.totalMemory;
            this.totalChildCount += node.totalChildCount;
        }

        public int GetChildIndexInList()
        {
            for (int i = 0; i < this.parent.children.Count; i++)
            {
                if (this.parent.children[i] == this)
                {
                    return i;
                }
            }
            return this.parent.children.Count;
        }

        public MemoryElement GetPrevNode()
        {
            int num = this.GetChildIndexInList() - 1;
            if (num >= 0)
            {
                MemoryElement memoryElement = this.parent.children[num];
                while (memoryElement.expanded)
                {
                    memoryElement = memoryElement.children[memoryElement.children.Count - 1];
                }
                return memoryElement;
            }
            return this.parent;
        }

        public MemoryElement GetNextNode()
        {
            if (this.expanded && this.children.Count > 0)
            {
                return this.children[0];
            }
            int num = this.GetChildIndexInList() + 1;
            if (num < this.parent.children.Count)
            {
                return this.parent.children[num];
            }
            MemoryElement memoryElement = this.parent;
            while (memoryElement.parent != null)
            {
                int num2 = memoryElement.GetChildIndexInList() + 1;
                if (num2 < memoryElement.parent.children.Count)
                {
                    return memoryElement.parent.children[num2];
                }
                memoryElement = memoryElement.parent;
            }
            return null;
        }

        public MemoryElement GetRoot()
        {
            if (this.parent != null)
            {
                return this.parent.GetRoot();
            }
            return this;
        }

        public MemoryElement FirstChild()
        {
            return this.children[0];
        }

        public MemoryElement LastChild()
        {
            if (!this.expanded)
            {
                return this;
            }
            return this.children[this.children.Count - 1].LastChild();
        }
    }
}
