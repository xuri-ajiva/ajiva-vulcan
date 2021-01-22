#define LOGGING_TRUE
using System;
using System.Diagnostics;

namespace ajiva.Helpers
{
    public abstract class DisposingLogger : IDisposable
    {
        protected readonly object disposeLock = new();
        protected bool disposed { get; private set; }

#if LOGGING_TRUE
        public DisposingLogger()
        {
            Console.WriteLine($"Created: {GetType()}");
        }
#endif
#region IDisposable

        protected abstract void ReleaseUnmanagedResources();

        [DebuggerStepThrough]
        protected virtual void Dispose(bool disposing)
        {
            lock (disposeLock)
            {
                if (disposed) return;
                ReleaseUnmanagedResources();
                disposed = true;
            }
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Dispose()
        {
#if LOGGING_TRUE
            Console.WriteLine($"Disposed: {GetType()}");
#endif
            Dispose(true);
            GC.SuppressFinalize(this);
            GC.Collect();
        }

        /// <inheritdoc />
        ~DisposingLogger()
        {
            Console.WriteLine($"Deleted: {GetType()}");
            Dispose(false);
        }

#endregion
    }
}
