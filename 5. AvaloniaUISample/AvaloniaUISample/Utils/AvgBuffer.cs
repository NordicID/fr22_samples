using System;
using System.Collections.Generic;

namespace AvaloniaUISample.Utils
{
    // Calculates average over specified time
    internal class AvgBuffer
    {
        internal class Entry
        {
            public long time;
            public double val;
        }

        LinkedList<Entry> mValues = new LinkedList<Entry>();
        int mMaxAge = 0;
        int mMaxSize = 10;

        private double mAvgValue = 0;//Double.NaN;
        private double mSumValue = 0;//Double.NaN;

        public AvgBuffer(int maxSize, int maxAge)
        {
            mMaxSize = maxSize;
            mMaxAge = maxAge;
        }

        bool RemoveOld()
        {
            if (mMaxAge == 0)
                return false;

            bool ret = false;
            LinkedListNode<Entry> ?node = mValues.Last;
            while (node != null)
            {
                LinkedListNode<Entry> ?prev = node.Previous;
                Entry e = node.Value;
                if (Environment.TickCount - e.time > mMaxAge)
                {
                    mValues.Remove(node);
                    ret = true;
                }
                node = prev;
            }
            return ret;
        }

        public double AvgValue
        {
            get
            {
                return mAvgValue;
            }
        }

        public double SumValue
        {
            get
            {
                return mSumValue;
            }
        }

        void CalcAvg()
        {
            if (mValues.Count == 0)
            {
                mAvgValue = 0;//Double.NaN;
                mSumValue = 0;
                return;
            }

            double avgVal = 0;
            LinkedListNode<Entry> ?node = mValues.First;
            while (node != null)
            {
                avgVal += node.Value.val;
                node = node.Next;
            }
            mSumValue = avgVal;
            mAvgValue = avgVal / mValues.Count;
        }

        public void Add(double val)
        {
            RemoveOld();

            while (mValues.Count >= mMaxSize)
            {
                mValues.RemoveFirst();
            }

            Entry e = new Entry()
            {
                time = Environment.TickCount,
                val = val
            };
            mValues.AddLast(e);

            CalcAvg();
        }

        public void Clear()
        {
            mValues.Clear();
            mAvgValue = 0;//Double.NaN;
            mSumValue = 0;
        }
    }
}
