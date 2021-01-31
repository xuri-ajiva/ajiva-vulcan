#define LOGGING_TRUE
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ajiva.Helpers
{
    public abstract class DisposingLogger : IDisposable
    {
        protected readonly object disposeLock = new();
        public bool Disposed { get; private set; }

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
            if (!disposing)
                Console.WriteLine($"Deleted: {GetType()}");
#if LOGGING_TRUE
            else
                Console.WriteLine($"Disposed: {GetType()}");
#endif

            lock (disposeLock)
            {
                if (Disposed) return;
                if (!disposing)
                    try
                    {
#if LOGGING_TRUE
                        Console.WriteLine("Trying to Release resources although we are not disposing!");
#endif
                        ReleaseUnmanagedResources();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error releasing unmanaged resources: " + e);
                    }
                else
                    ReleaseUnmanagedResources();
                Disposed = true;
            }
        }

        [DebuggerStepThrough]
        public void DisposeIn(int delayMs)
        {
            GC.SuppressFinalize(this);
            Task.Run(async () =>
            {
                await Task.Delay(delayMs);
                Dispose(true);
            });
        }

        /// <inheritdoc />
        [DebuggerStepThrough]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        ~DisposingLogger()
        {
            Dispose(false);
        }

#endregion
    }
}
