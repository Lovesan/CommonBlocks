using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CommonBlocks
{
    /// <summary>
    /// Generic lock-free queue implementation. Michael and Scott algorithm.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    public class NonBlockingQueue<T> : IProducerConsumerCollection<T>
    {
        private class Node
        {
            public T value;

            public volatile Node next;

            public Node() { }

            public Node(T val) { value = val; }

            public IEnumerable<T> AsEnumerable()
            {
                var n = this;
                while(n != null)
                {
                    yield return n.value;
                    n = n.next;
                }
            }
        }

        /// <summary>
        /// Queue head. Never null. Initialized with dummy element.
        /// </summary>
        private volatile Node _head;
        /// <summary>
        /// Queue tail. Never null.
        /// </summary>
        private volatile Node _tail;
        /// <summary>
        /// Queue size
        /// </summary>
        private int _count;

        /// <summary>
        /// Initialized NonBlockingQueue from <paramref name="collection"/> supplied.
        /// </summary>
        /// <param name="collection">Collection holding initial queue elements.</param>
        public NonBlockingQueue(IEnumerable<T> collection)
        {
            _head = new Node();
            _tail = _head;
            _count = 0;
            if (collection == null)
                collection = Enumerable.Empty<T>();
            foreach(var x in collection)
            {
                var n = new Node(x);
                _tail.next = n;
                _tail = n;
                ++_count;
            }
        }

        /// <summary>
        /// Initialized new empty NonBlockingQueue.
        /// </summary>
        public NonBlockingQueue()
        {
            _head = _tail = new Node();
            _count = 0;
        }

        /// <summary>
        /// Queue size.
        /// </summary>
        public int Count { get { return _count; } }

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
        /// Atomically adds element to the back of the queue.
        /// </summary>
        /// <param name="value">Value to be added.</param>
        public void Enqueue(T value)
        {
            Node tail, next, node = new Node(value);
            var inserting = true;
            do
            {
                // Prepare data for transactional modification of the queue.
                tail = _tail;
                next = tail.next;
                if(tail == _tail) // Validate queue consistency.
                {
                    if(next == null)
                    {
                        // Tail node is consistent. Try to set its next element transactionally.
                        if (Interlocked.CompareExchange(ref tail.next, node, next) == next)
                            // Transaction succeeded. Stop transaction loop.
                            inserting = false;
                    }
                    else
                    {
                        // Queue tail is inconsistent. Help other threads setting it to right value.
                        Interlocked.CompareExchange(ref _tail, next, tail);
                    }
                }
            } while (inserting);
            // Try setting queue tail to newly allocated element.
            Interlocked.CompareExchange(ref _tail, node, tail);
            // Increment size counter
            Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// Atomically removes element from the front of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Queue is empty.</exception>
        /// <returns>Value removed.</returns>
        public T Dequeue()
        {
            T value;
            if (!TryDequeue(out value))
                throw new InvalidOperationException();
            return value;
        }

        /// <summary>
        /// Atomically removes element from the from of the queue.
        /// </summary>
        /// <param name="value">Value removed</param>
        /// <returns>True if queue was not empty.</returns>
        public bool TryDequeue(out T value)
        {
            Node head, tail, next;
            var removing = true;
            value = default(T);
            do
            {
                // Prepare data for transactional modification of the queue.
                head = _head;
                tail = _tail;
                next = head.next;
                if(head == _head) // Validate queue consistency.
                {
                    if(head == tail)
                    {
                        // Queue is empty.
                        if (next == null)
                            return false;
                        // Queue is not empty but queue tail is inconsistent.
                        // Help other threads to set it to right value.
                        Interlocked.CompareExchange(ref _tail, next, tail);
                    }
                    else
                    {
                        // Queue head is consistent. Try remove it from the queue.
                        // Transaction will succeed only if current queue head is equal
                        //     to the node we retrieved before.
                        value = next.value;
                        if (Interlocked.CompareExchange(ref _head, next, head) == head)
                            // Transaction succeeded. Stop the loop.
                            removing = false;
                    }
                }
            } while (removing);
            // Decrement size counter.
            Interlocked.Decrement(ref _count);
            return true;
        }

        /// <summary>
        /// Atomically retrieves element from the front of the queue without modifying it.
        /// </summary>
        /// <exception cref="InvalidOperationException">Queue is empty.</exception>
        /// <returns>Value retrieved.</returns>
        public T Peek()
        {
            T value;
            if (!TryPeek(out value))
                throw new InvalidOperationException();
            return value;
        }

        /// <summary>
        /// Atomically retrieves element from the front of the queue without modifying it.
        /// </summary>
        /// <param name="value">Value retrieved</param>
        /// <returns>True if queue was not empty</returns>
        public bool TryPeek(out T value)
        {
            var next = _head.next;
            value = next == null ? default(T) : next.value;
            return next != null;
        }
        
        /// <summary>
        /// Removes all elements from the queue.
        /// </summary>
        public void Clear()
        {
            T value;
            while (TryDequeue(out value))
                ;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach(var x in this)
            {
                array[arrayIndex++] = x;
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            var first = _head.next;
            if (first == null)
                return Enumerable.Empty<T>().GetEnumerator();
            return first.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Contains(T value)
        {
            return this.AsEnumerable().Contains(value);
        }

        public bool TryAdd(T item)
        {
            Enqueue(item);
            return true;
        }

        public bool TryTake(out T item)
        {
            return TryDequeue(out item);
        }

        public T[] ToArray()
        {
            return this.AsEnumerable().ToArray();
        }

        public void CopyTo(Array array, int index)
        {
            foreach(var x in this)
            {
                array.SetValue(x, index++);
            }
        }
    }
}
