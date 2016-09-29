using System;

namespace CoInternal
{
    [Serializable]
    class MemoryElementSelection
    {
        private MemoryElement m_Selected;

        public MemoryElement Selected
        {
            get
            {
                return this.m_Selected;
            }
        }

        public void SetSelection(MemoryElement node)
        {
            this.m_Selected = node;
            for (MemoryElement parent = node.parent; parent != null; parent = parent.parent)
            {
                parent.expanded = true;
            }
        }

        public void ClearSelection()
        {
            this.m_Selected = null;
        }

        public bool isSelected(MemoryElement node)
        {
            return this.m_Selected == node;
        }

        public void MoveUp()
        {
            if (this.m_Selected == null)
            {
                return;
            }
            if (this.m_Selected.parent == null)
            {
                return;
            }
            MemoryElement prevNode = this.m_Selected.GetPrevNode();
            if (prevNode.parent != null)
            {
                this.SetSelection(prevNode);
            }
            else
            {
                this.SetSelection(prevNode.FirstChild());
            }
        }

        public void MoveDown()
        {
            if (this.m_Selected == null)
            {
                return;
            }
            if (this.m_Selected.parent == null)
            {
                return;
            }
            MemoryElement nextNode = this.m_Selected.GetNextNode();
            if (nextNode != null)
            {
                this.SetSelection(nextNode);
            }
        }

        public void MoveFirst()
        {
            if (this.m_Selected == null)
            {
                return;
            }
            if (this.m_Selected.parent == null)
            {
                return;
            }
            this.SetSelection(this.m_Selected.GetRoot().FirstChild());
        }

        public void MoveLast()
        {
            if (this.m_Selected == null)
            {
                return;
            }
            if (this.m_Selected.parent == null)
            {
                return;
            }
            this.SetSelection(this.m_Selected.GetRoot().LastChild());
        }

        public void MoveParent()
        {
            if (this.m_Selected == null)
            {
                return;
            }
            if (this.m_Selected.parent == null)
            {
                return;
            }
            if (this.m_Selected.parent.parent == null)
            {
                return;
            }
            this.SetSelection(this.m_Selected.parent);
        }
    }
}
