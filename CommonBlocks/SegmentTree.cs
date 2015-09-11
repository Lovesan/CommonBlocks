using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CommonBlocks
{
    /// <summary>
    /// Binary tree holding results of some operation on array segments.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    public class SegmentTree<T> : IReadOnlyList<T>
    {
        private class Node
        {
            public T Data { get; private set; }

            public Node LeftChild { get; private set; }

            public Node RightChild { get; private set; }

            public int Left { get; private set; }

            public int Right { get; private set; }

            public Node(T data, int left, int right, Node leftChild, Node rightChild)
            {
                Data = data;
                Left = left;
                Right = right;
                LeftChild = leftChild;
                RightChild = rightChild;
            }

            public Node(T data, int left, int right)
                : this(data, left, right, null, null)
            { }
        }

        private Node _root;
        private int _len;
        private readonly Func<T, T, T> _valueFactory;

        public SegmentTree(IEnumerable<T> data, Func<T, T, T> valueFactory)
        {
            if (data == null)
                data = Enumerable.Empty<T>();
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");
            var list = new List<T>(data);
            _len = list.Count;
            _valueFactory = valueFactory;
            _root = CreateTree(list, valueFactory, 0, list.Count - 1);
        }

        public T this[int index]
        {
            get { return Get(index, index); }
            set
            {
                if (index < 0 || index >= _len)
                    throw new ArgumentOutOfRangeException("index");
                _root = Update(_root, index, value);
            }
        }

        public int Count { get { return _len; } }

        public T Get(int left, int right)
        {
            if(left > right || left < 0)
                throw new ArgumentOutOfRangeException("left");
            if (right < 0 || right >= _len)
                throw new ArgumentOutOfRangeException("right");
            return Get(_root, left, right);
        }

        private T Get(Node n, int l, int r)
        {
            if (n.Left == l && n.Right == r)
                return n.Data;
            var m = n.Left + (n.Right - n.Left) / 2;
            var dR = Math.Min(r, m);
            var dL = Math.Max(m + 1, l);
            if (l > dR)
                return Get(n.RightChild, dL, r);
            if (dL > r)
                return Get(n.LeftChild, l, dR);
            var left = Get(n.LeftChild, l, dR);
            var right = Get(n.RightChild, dL, r);
            return _valueFactory(left, right);
        }

        private Node Update(Node n, int i, T val)
        {
            if (n.Left > i)
                return n;
            if (n.Right < i)
                return n;
            if (n.Left == i && n.Right == i)
                return new Node(val, i, i);
            var left = Update(n.LeftChild, i, val);
            var right = Update(n.RightChild, i, val);
            return new Node(_valueFactory(left.Data, right.Data), n.Left, n.Right, left, right);
        }

        private static Node CreateTree(IList<T> data, Func<T,T,T> vf, int l, int r)
        {
            if (l == r)
                return new Node(data[l], l, r);
            var m = l + (r - l) / 2;
            var left = CreateTree(data, vf, l, m);
            var right = CreateTree(data, vf, m + 1, r);
            return new Node(vf(left.Data, right.Data), l, r, left, right);
        }

        private IEnumerable<T> AsEnumerable()
        {
            var curr = _root;
            var q = new Stack<Node>();
            if (curr != null)
                q.Push(curr);
            while(q.Count > 0)
            {
                curr = q.Pop();
                if(curr.Left == curr.Right)
                    yield return curr.Data;
                if (curr.RightChild != null)
                    q.Push(curr.RightChild);
                if (curr.LeftChild != null)
                    q.Push(curr.LeftChild);             
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }
    }
}
