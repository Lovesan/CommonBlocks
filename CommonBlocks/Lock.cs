using System;
using System.Threading;

namespace CommonBlocks
{
    /// <summary>
    /// Fast reentrant mutex silimiar to Windows CRITICAL_SECTION.
    ///  Goes into the kernel only in case of lock contention.
    /// </summary>
    public class Lock : IDisposable
    {
        /// <summary>
        /// Semaphore used in case of lock contention
        /// </summary>
        private readonly Semaphore _sem;
        /// <summary>
        /// Id of the thread that is owning the lock.
        /// </summary>
        private int _owner;
        /// <summary>
        /// Lock count.
        /// </summary>
        private int _count;
        /// <summary>
        /// Recursion count.
        /// </summary>
        private int _recursion;
        /// <summary>
        /// Is object disposed.
        /// </summary>
        private bool _disposed;

        public Lock()
        {
            _sem = new Semaphore(0, 1);
            _count = 0;
            _recursion = 0;
            _owner = 0;
            _disposed = false;
        }

        /// <summary>
        /// Acquires the lock.
        /// </summary>
        /// <returns><see cref="IDisposable"/> object used to release the lock</returns>
        public IDisposable Acquire()
        {
            var t = Thread.CurrentThread.ManagedThreadId;
            // Try to acquire lock. In case of either lock count is zero or 
            //  `_owner` is equal to current thread, we can skip semaphore acquisition.
            if(Interlocked.Increment(ref _count) > 1 && _owner != t)
            {
                // Lock has been acquired by another thread. Wait on semaphore.
                _sem.WaitOne();
            }
            // Set owning thread to current thread.
            _owner = t;
            ++_recursion;
            return new DelegateDisposable(Release);
        }

        public bool TryAcquire()
        {
            IDisposable release;
            return TryAcquire(out release);
        }

        public bool TryAcquire(out IDisposable release)
        {
            var t = Thread.CurrentThread.ManagedThreadId;
            release = null;
            if(_owner == t)
            {
                // We've already acquired the lock.
                Interlocked.Increment(ref _count);
            }
            else
            {
                // In case of current lock count equals to zero, we can acquire the lock.
                if (Interlocked.CompareExchange(ref _count, 1, 0) != 0)
                    return false;
                _owner = t;
            }
            release = new DelegateDisposable(Release);
            return true;
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <exception cref="SynchronizationLockException">Lock is not owned by current thread.</exception>
        public void Release()
        {
            var t = Thread.CurrentThread.ManagedThreadId;
            if (_owner != t)
                throw new SynchronizationLockException();
            var recur = --_recursion;
            if (recur == 0)
                _owner = 0;
            if(Interlocked.Decrement(ref _count) > 0 && recur == 0)
            {
                _sem.Release();
            }
        }

        ~Lock()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                _sem.Dispose();
            }
        }
    }
}
