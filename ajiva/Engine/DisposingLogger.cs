//#define LOGGING_TRUE
using System;

namespace ajiva.Engine
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
        public void Dispose()
        {
#if LOGGING_TRUE
            Console.WriteLine($"Disposed: {GetType()}");
#endif
            Dispose(true);
            GC.SuppressFinalize(this);
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
