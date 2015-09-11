using System;

namespace CommonBlocks
{
    /// <summary>
    /// Readers-writers lock with reader preference.
    /// </summary>
    public class ReadersLock : IDisposable
    {
        private bool _diposed;
        private int _readers;
        private readonly Lock _lockRead;
        private readonly Lock _lockWrite;
        private IDisposable _writer;

        public ReadersLock()
        {
            _diposed = false;
            _readers = 0;
            _lockRead = new Lock();
            _lockWrite = new Lock();
        }

        public IDisposable AcquireRead()
        {
            using (_lockRead.Acquire())
            {
                if (++_readers == 1)
                    _writer = _lockWrite.Acquire();
            }
            return new DelegateDisposable(ExitRead);
        }

        private void ExitRead()
        {
            using (_lockRead.Acquire())
            {
                if (--_readers == 0)
                    _writer.Dispose();
            }
        }

        public IDisposable AcquireWrite()
        {
            _writer = _lockWrite.Acquire();
            return new DelegateDisposable(ExitWrite);
        }

        private void ExitWrite()
        {
            _writer.Dispose();
        }

        ~ReadersLock()
        {
            Dispose();
        }

        public void Dispose()
        {
            if(!_diposed)
            {
                _diposed = true;
                _lockRead.Dispose();
                _lockWrite.Dispose();
            }
        }
    }
}
