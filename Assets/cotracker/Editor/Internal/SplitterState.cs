using System;
using UnityEngine;

namespace CoInternal
{
    [Serializable]
    class SplitterState
    {
        private const int defaultSplitSize = 6;

        public int ID;

        public int splitterInitialOffset;

        public int currentActiveSplitter = -1;

        public int[] realSizes;

        public float[] relativeSizes;

        public int[] minSizes;

        public int[] maxSizes;

        public int lastTotalSize;

        public int splitSize;

        public float xOffset;

        public SplitterState(params float[] relativeSizes)
        {
            this.Init(relativeSizes, null, null, 0);
        }

        public SplitterState(int[] realSizes, int[] minSizes, int[] maxSizes)
        {
            this.realSizes = realSizes;
            this.minSizes = ((minSizes != null) ? minSizes : new int[realSizes.Length]);
            this.maxSizes = ((maxSizes != null) ? maxSizes : new int[realSizes.Length]);
            this.relativeSizes = new float[realSizes.Length];
            this.splitSize = ((this.splitSize != 0) ? this.splitSize : 6);
            this.RealToRelativeSizes();
        }

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            this.Init(relativeSizes, minSizes, maxSizes, 0);
        }

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
        {
            this.Init(relativeSizes, minSizes, maxSizes, splitSize);
        }

        private void Init(float[] relativeSizes, int[] minSizes, int[] maxSizes, int splitSize)
        {
            this.relativeSizes = relativeSizes;
            this.minSizes = ((minSizes != null) ? minSizes : new int[relativeSizes.Length]);
            this.maxSizes = ((maxSizes != null) ? maxSizes : new int[relativeSizes.Length]);
            this.realSizes = new int[relativeSizes.Length];
            this.splitSize = ((splitSize != 0) ? splitSize : 6);
            this.NormalizeRelativeSizes();
        }

        public void NormalizeRelativeSizes()
        {
            float num = 1f;
            float num2 = 0f;
            for (int i = 0; i < this.relativeSizes.Length; i++)
            {
                num2 += this.relativeSizes[i];
            }
            for (int i = 0; i < this.relativeSizes.Length; i++)
            {
                this.relativeSizes[i] = this.relativeSizes[i] / num2;
                num -= this.relativeSizes[i];
            }
            this.relativeSizes[this.relativeSizes.Length - 1] += num;
        }

        public void RealToRelativeSizes()
        {
            float num = 1f;
            float num2 = 0f;
            for (int i = 0; i < this.realSizes.Length; i++)
            {
                num2 += (float)this.realSizes[i];
            }
            for (int i = 0; i < this.realSizes.Length; i++)
            {
                this.relativeSizes[i] = (float)this.realSizes[i] / num2;
                num -= this.relativeSizes[i];
            }
            if (this.relativeSizes.Length > 0)
            {
                this.relativeSizes[this.relativeSizes.Length - 1] += num;
            }
        }

        public void RelativeToRealSizes(int totalSpace)
        {
            int num = totalSpace;
            for (int i = 0; i < this.relativeSizes.Length; i++)
            {
                this.realSizes[i] = (int)Mathf.Round(this.relativeSizes[i] * (float)totalSpace);
                if (this.realSizes[i] < this.minSizes[i])
                {
                    this.realSizes[i] = this.minSizes[i];
                }
                num -= this.realSizes[i];
            }
            if (num < 0)
            {
                for (int i = 0; i < this.relativeSizes.Length; i++)
                {
                    if (this.realSizes[i] > this.minSizes[i])
                    {
                        int num2 = this.realSizes[i] - this.minSizes[i];
                        int num3 = (-num >= num2) ? num2 : (-num);
                        num += num3;
                        this.realSizes[i] -= num3;
                        if (num >= 0)
                        {
                            break;
                        }
                    }
                }
            }
            int num4 = this.realSizes.Length - 1;
            if (num4 >= 0)
            {
                this.realSizes[num4] += num;
                if (this.realSizes[num4] < this.minSizes[num4])
                {
                    this.realSizes[num4] = this.minSizes[num4];
                }
            }
        }

        public void DoSplitter(int i1, int i2, int diff)
        {
            int num = this.realSizes[i1];
            int num2 = this.realSizes[i2];
            int num3 = this.minSizes[i1];
            int num4 = this.minSizes[i2];
            int num5 = this.maxSizes[i1];
            int num6 = this.maxSizes[i2];
            bool flag = false;
            if (num3 == 0)
            {
                num3 = 16;
            }
            if (num4 == 0)
            {
                num4 = 16;
            }
            if (num + diff < num3)
            {
                diff -= num3 - num;
                this.realSizes[i2] += this.realSizes[i1] - num3;
                this.realSizes[i1] = num3;
                if (i1 != 0)
                {
                    this.DoSplitter(i1 - 1, i2, diff);
                }
                else
                {
                    this.splitterInitialOffset -= diff;
                }
                flag = true;
            }
            else if (num2 - diff < num4)
            {
                diff -= num2 - num4;
                this.realSizes[i1] += this.realSizes[i2] - num4;
                this.realSizes[i2] = num4;
                if (i2 != this.realSizes.Length - 1)
                {
                    this.DoSplitter(i1, i2 + 1, diff);
                }
                else
                {
                    this.splitterInitialOffset -= diff;
                }
                flag = true;
            }
            if (!flag)
            {
                if (num5 != 0 && num + diff > num5)
                {
                    diff -= this.realSizes[i1] - num5;
                    this.realSizes[i2] += this.realSizes[i1] - num5;
                    this.realSizes[i1] = num5;
                    if (i1 != 0)
                    {
                        this.DoSplitter(i1 - 1, i2, diff);
                    }
                    else
                    {
                        this.splitterInitialOffset -= diff;
                    }
                    flag = true;
                }
                else if (num6 != 0 && num2 - diff > num6)
                {
                    diff -= num2 - num6;
                    this.realSizes[i1] += this.realSizes[i2] - num6;
                    this.realSizes[i2] = num6;
                    if (i2 != this.realSizes.Length - 1)
                    {
                        this.DoSplitter(i1, i2 + 1, diff);
                    }
                    else
                    {
                        this.splitterInitialOffset -= diff;
                    }
                    flag = true;
                }
            }
            if (!flag)
            {
                this.realSizes[i1] += diff;
                this.realSizes[i2] -= diff;
            }
        }
    }
}
