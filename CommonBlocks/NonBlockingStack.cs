using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CommonBlocks
{
    /// <summary>
    /// Generic lock-free stack implementation
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    public class NonBlockingStack<T> : IProducerConsumerCollection<T>
    {
        private class Node
        {
            public T Value { get; set; }

            public Node Next { get; set; }

            public Node(T value, Node next) { Value = value; Next = next; }

            public Node(T value) { Value = value; Next = null; }

            public IEnumerable<T> AsEnumerable()
            {
                var n = this;
                while(n != null)
                {
                    yield return n.Value;
                    n = n.Next;
                }
            }
        }

        /// <summary>
        /// Stack root.
        /// </summary>
        private Node _list;
        /// <summary>
        /// Stack size.
        /// </summary>
        private int _count;

        /// <summary>
        /// Initialized new NonBlockingStack from <paramref name="collection"/> supplied.
        /// </summary>
        /// <param name="collection">Collection holding initial elements of the stack</param>
        public NonBlockingStack(IEnumerable<T> collection)
        {
            if (collection == null)
                collection = Enumerable.Empty<T>();
            _count = 0;
            _list = null;
            foreach(var x in collection)
            {
                _list = new Node(x, _list);
                ++_count;
            }
        }

        /// <summary>
        /// Initialized new empty NonBlockingStack
        /// </summary>
        public NonBlockingStack()
        {
            _count = 0;
            _list = null;
        }

        /// <summary>
        /// Stack size.
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        public object SyncRoot
        {
            get
            {
                return null;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Atomically pushes value to stack.
        /// </summary>
        /// <param name="value">value to be pushed.</param>
        public void Push(T value)
        {
            Node list, next = new Node(value);
            do
            {
                // Prepare data for transactional modification of stack root.
                list = _list;
                next.Next = list;
            // Try to modify list root transcationally.
            // Set new list root to `next` only if current list root is equal to `list`.
            } while (Interlocked.CompareExchange(ref _list, next, list) != list);
            // Increment size counter.
            Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// Atomically removes value from the top of the stack
        /// </summary>
        /// <param name="value">value retrieved</param>
        /// <returns>True if stack was not empty</returns>
        public bool TryPop(out T value)
        {
            Node list, next;
            value = default(T);
            do
            {
                // Prepare data for transactional modification of stack root.
                list = _list;
                // Return immediately in case of empty stack.
                if (list == null)
                    return false;
                next = list.Next;
                value = list.Value;
            // Data is prepated. Try to modify stack root.
            // Interlocked transaction will succeed only if current stack root is equal to `list`.
            } while (Interlocked.CompareExchange(ref _list, next, list) != list);
            // Decrement size counter.
            Interlocked.Decrement(ref _count);
            return true;
        }

        /// <summary>
        /// Atomically removes value from the top of the stack.
        /// </summary>
        /// <exception cref="InvalidOperationException">Stack is empty</exception>
        /// <returns>Value removed</returns>
        public T Pop()
        {
            T value;
            if (!TryPop(out value))
                throw new InvalidOperationException();
            return value;
        }

        /// <summary>
        /// Atomically retrieves element from the stack top without modifying the stack.
        /// </summary>
        /// <param name="value">Value retrieved</param>
        /// <returns>True if stack was not empty.</returns>
        public bool TryPeek(out T value)
        {
            var list = _list;
            value = default(T);
            if (list == null)
                return false;
            value = list.Value;
            return true;
        }

        /// <summary>
        /// Atomically retrieves element from the stack top without modifying the stack.
        /// </summary>
        /// <exception cref="InvalidOperationException">Stack is empty.</exception>
        /// <returns>Value retrieved.</returns>
        public T Peek()
        {
            T value;
            if (!TryPeek(out value))
                throw new InvalidOperationException();
            return value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var list = _list;
            if (list == null)
                return Enumerable.Empty<T>().GetEnumerator();
            return list.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Atomically clears the stack.
        /// </summary>
        public void Clear()
        {
            Node n;
            do
            {
                n = _list;
            } while (Interlocked.CompareExchange(ref _list, null, n) != n);
        }

        public bool Contains(T item)
        {
            return this.AsEnumerable().Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach(var x in this)
            {
                array[arrayIndex++] = x;
            }
        }

        public bool TryAdd(T item)
        {
            Push(item);
            return true;
        }

        public bool TryTake(out T item)
        {
            return TryPop(out item);
        }

        public T[] ToArray()
        {
            return this.AsEnumerable().ToArray();
        }

        public void CopyTo(Array array, int index)
        {
            foreach (var x in this)
            {
                array.SetValue(x, index++);
            }         
        }
    }
}
