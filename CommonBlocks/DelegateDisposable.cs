using System;
using System.Threading;

namespace CommonBlocks
{
    /// <summary>
    /// Runs delegate on <see cref="IDisposable.Dispose"/>
    /// </summary>
    public class DelegateDisposable : IDisposable
    {
        private readonly bool _finalize;
        private Action _action;

        /// <summary>
        /// Initializes new <see cref="DelegateDisposable"/> using supplied action.
        /// </summary>
        /// <param name="action">Action to run on <see cref="Dispose"/>.</param>
        /// <param name="finalize">Whether to run action on finalization.</param>
        public DelegateDisposable(Action action, bool finalize)
        {
            _action = action;
            _finalize = finalize;
        }

        /// <summary>
        /// Initializes new <see cref="DelegateDisposable"/> using supplied action.
        /// </summary>
        /// <param name="action">Action to run on <see cref="Dispose"/>.</param>
        public DelegateDisposable(Action action)
            : this(action, false)
        { }

        ~DelegateDisposable()
        {
            if (_finalize)
                Dispose();
        }

        /// <summary>
        /// Runs action holded by this <see cref="DelegateDisposable"/>.
        /// </summary>
        public void Dispose()
        {
            var a = Interlocked.Exchange(ref _action, null);
            if (a == null)
                return;
            a();
        }
    }
}
