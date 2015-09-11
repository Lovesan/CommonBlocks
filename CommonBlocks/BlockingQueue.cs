using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Collections;

namespace CommonBlocks
{
    /// <summary>
    /// Generic blocking queue.
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    public class BlockingQueue<T> : IProducerConsumerCollection<T>, IReadOnlyCollection<T>
    {
        private Queue<T> _q;
        private int _cap;
        private object _lock;

        /// <summary>
        /// Initializes new empty BlockingQueue with the specified bounded capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of elements in the queue.</param>
        public BlockingQueue(int capacity)
        {
            if (capacity < 1)
                throw new IndexOutOfRangeException();
            _q = new Queue<T>();
            _cap = capacity;
            _lock = new object();
        }

        /// <summary>
        /// Initializes new empty unbounded BlockingQueue.
        /// </summary>
        public BlockingQueue()
        {
            _q = new Queue<T>();
            _cap = -1;
            _lock = new object();
        }

        /// <summary>
        /// Queue size
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _q.Count;
                }
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return true;
            }
        }

        public object SyncRoot
        {
            get
            {
                return _lock;
            }
        }

        /// <summary>
        /// Adds an elements to queue. Blocks in case when maximum capacity is reached.
        /// </summary>
        /// <param name="item">Value to be added</param>
        public void Enqueue(T item)
        {
            TryEnqueue(item, -1);
        }

        /// <summary>
        /// Adds an element to queue.
        /// Blocks in case when maximum capacity is reached until <paramref name="timeout"/> elapses.
        /// </summary>
        /// <param name="item">Value to be added</param>
        /// <param name="timeout">Operation timeout</param>
        /// <returns>True if element was successfully added to queue. False if timeout elapsed.</returns>
        public bool TryEnqueue(T item, TimeSpan timeout)
        {
            return TryEnqueue(item, (int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Adds an element to queue.
        /// Blocks in case when maximum capacity is reached until <paramref name="timeoutMs"/> elapses.
        /// </summary>
        /// <param name="item">Value to be added</param>
        /// <param name="timeoutMs">Operation timeout, in milliseconds.</param>
        /// <returns>True if element was successfully added to queue. False if timeout elapsed.</returns>
        public bool TryEnqueue(T item, int timeoutMs)
        {
            lock(_lock)
            {
                if(_cap > 0)
                {
                    if (timeoutMs < 0)
                    {
                        while (_q.Count >= _cap)
                        {
                            Monitor.Wait(_lock);
                        }
                    }
                    else
                    {
                        var tm = Environment.TickCount;
                        while(_q.Count >= _cap)
                        {
                            if (timeoutMs < 0 || !Monitor.Wait(_lock, timeoutMs))
                                return false;
                            timeoutMs -= (Environment.TickCount - tm);
                        }
                    }
                }
                _q.Enqueue(item);
                if (_q.Count == 1)
                    Monitor.PulseAll(_lock);
                return true;
            }
        }

        /// <summary>
        /// Removes an element from queue. Blocks until an element is available.
        /// </summary>
        /// <returns>An element removed.</returns>
        public T Dequeue()
        {
            T item;
            TryDequeue(out item, -1);
            return item;
        }

        /// <summary>
        /// Removes an element from queue.
        /// Blocks for the specified <paramref name="timeout"/> until an element is available.
        /// </summary>
        /// <param name="item">Element retrieved.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>True if an element was successfully retrieved. False if <paramref name="timeout"/> elapsed.</returns>
        public bool TryDequeue(out T item, TimeSpan timeout)
        {
            return TryDequeue(out item, (int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Removes an element from queue.
        /// Blocks for the specified <paramref name="timeoutMs"/> until an element is available.
        /// </summary>
        /// <param name="item">Element retrieved.</param>
        /// <param name="timeoutMs">Operation timeout, in milliseconds.</param>
        /// <returns>True if an element was successfully retrieved. False if <paramref name="timeoutMs"/> elapsed.</returns>
        public bool TryDequeue(out T item, int timeoutMs)
        {
            lock(_lock)
            {
                item = default(T);
                if(timeoutMs < 0)
                {
                    while (_q.Count == 0)
                    {
                        Monitor.Wait(_lock);
                    }
                }
                else
                {
                    var tm = Environment.TickCount;
                    while(_q.Count == 0)
                    {
                        if (timeoutMs < 0 || !Monitor.Wait(_lock, timeoutMs))
                            return false;
                        timeoutMs -= (Environment.TickCount - tm);
                    }
                }
                item = _q.Dequeue();
                if (_cap > 0 && _q.Count == _cap - 1)
                    Monitor.PulseAll(_lock);
                return true;
            }
        }

        /// <summary>
        /// Copies queue contens to specified <paramref name="array"/> starting at array <paramref name="index"/>.
        /// </summary>
        /// <param name="array">An array</param>
        /// <param name="index">Index of the first replaced element in <paramref name="array"/>.</param>
        public void CopyTo(Array array, int index)
        {
            lock (_lock)
            {
                foreach (var x in _q)
                {
                    array.SetValue(x, index++);
                }
            }
        }

        /// <summary>
        /// Copies queue contens to specified <paramref name="array"/> starting at array <paramref name="index"/>.
        /// </summary>
        /// <param name="array">An array</param>
        /// <param name="index">Index of the first replaced element in <paramref name="array"/>.</param>
        public void CopyTo(T[] array, int index)
        {
            lock(_lock)
            {
                _q.CopyTo(array, index);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _q.GetEnumerator();
        }

        /// <summary>
        /// Converts queue content to an array.
        /// </summary>
        /// <returns>Array holding queue contents.</returns>
        public T[] ToArray()
        {
            lock(_lock)
            {
                return _q.ToArray();
            }
        }

        /// <summary>
        /// Adds an element to the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Queue is full.</exception>
        /// <param name="item">Element to be added.</param>
        public void Add(T item)
        {
            if (!TryAdd(item))
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Adds an element to the queue.
        /// </summary>
        /// <param name="item">Element to be added</param>
        /// <returns>True if an element was successfully added. False is queue is full.</returns>
        public bool TryAdd(T item)
        {
            lock(_lock)
            {
                if (_cap > 0 && _q.Count >= _cap)
                    return false;
                _q.Enqueue(item);
                if (_q.Count == 1)
                    Monitor.PulseAll(_lock);
                return true;
            }
        }

        /// <summary>
        /// Retrieves an element from the front of the queue without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">Queue is full.</exception>
        /// <returns>An element at the front of the queue.</returns>
        public T Peek()
        {
            T item;
            if (!TryPeek(out item))
                throw new InvalidOperationException();
            return item;
        }

        /// <summary>
        /// Retrieves an element from the front of the queue without removing it.
        /// </summary>
        /// <param name="item">Element at the front of the queue.</param>
        /// <returns>True if queue is not empty, false otherwise.</returns>
        public bool TryPeek(out T item)
        {
            lock(_lock)
            {
                item = default(T);
                if (_q.Count == 0)
                    return false;
                item = _q.Peek();
                return true;
            }
        }

        /// <summary>
        /// Removes an element from the front of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Queue is empty.</exception>
        /// <returns>Element removed.</returns>
        public T Take()
        {
            T item;
            if (!TryTake(out item))
                throw new InvalidOperationException();
            return item;
        }

        /// <summary>
        /// Removes an element from the front of the queue.
        /// </summary>
        /// <param name="item">Element removed.</param>
        /// <returns>True if an element was successfully removed. False if queue is empty.</returns>
        public bool TryTake(out T item)
        {
            lock(_lock)
            {
                item = default(T);
                if (_q.Count == 0)
                    return false;
                item = _q.Dequeue();
                if (_cap > 0 && _q.Count == _cap - 1)
                    Monitor.PulseAll(_lock);
                return true;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Removes all elements from the queue.
        /// </summary>
        public void Clear()
        {
            lock(_lock)
            {
                var size = _q.Count();
                _q.Clear();
                if (_cap > 0 && size == _cap - 1)
                    Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Finds out whether an <paramref name="item"/> exists in the queue.
        /// </summary>
        /// <param name="item">Item to be found.</param>
        /// <returns>True if an <paramref name="item"/> exists in the queue.</returns>
        public bool Contains(T item)
        {
            lock(_lock)
            {
                return _q.Contains(item);
            }
        }
    }
}
