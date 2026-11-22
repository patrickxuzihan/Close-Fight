using System;

namespace CloseFight.Core.Path
{
    /// <summary>
    /// Binary min-heap of (node, priority). No decrease-key: callers push duplicates
    /// and skip stale entries when popped. (.NET Standard 2.1 has no PriorityQueue.)
    /// </summary>
    public sealed class MinHeap
    {
        int[] _nodes;
        float[] _priorities;

        public int Count { get; private set; }

        public MinHeap(int capacity)
        {
            capacity = Math.Max(capacity, 4);
            _nodes = new int[capacity];
            _priorities = new float[capacity];
        }

        public void Clear()
        {
            Count = 0;
        }

        public void Push(int node, float priority)
        {
            if (Count == _nodes.Length)
            {
                Array.Resize(ref _nodes, Count * 2);
                Array.Resize(ref _priorities, Count * 2);
            }

            int i = Count++;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_priorities[parent] <= priority)
                {
                    break;
                }

                _nodes[i] = _nodes[parent];
                _priorities[i] = _priorities[parent];
                i = parent;
            }

            _nodes[i] = node;
            _priorities[i] = priority;
        }

        public int Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }

            int top = _nodes[0];
            Count--;
            if (Count > 0)
            {
                int lastNode = _nodes[Count];
                float lastPriority = _priorities[Count];
                int i = 0;
                while (true)
                {
                    int left = 2 * i + 1;
                    if (left >= Count)
                    {
                        break;
                    }

                    int right = left + 1;
                    int child = right < Count && _priorities[right] < _priorities[left] ? right : left;
                    if (_priorities[child] >= lastPriority)
                    {
                        break;
                    }

                    _nodes[i] = _nodes[child];
                    _priorities[i] = _priorities[child];
                    i = child;
                }

                _nodes[i] = lastNode;
                _priorities[i] = lastPriority;
            }

            return top;
        }
    }
}
