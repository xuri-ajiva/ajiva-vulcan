#define LOGGING_TRUE
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ajiva.Wrapper.Logger;

namespace ajiva.Helpers
{
    public abstract class DisposingLogger : IDisposable
    {
        protected readonly object disposeLock = new();
        public bool Disposed { get; private set; }

        private static readonly ConsoleRolBlock Block = new(10, nameof(DisposingLogger));

        static void Log(string msg)
        {
            //LogHelper.Log(msg);
            //Block.WriteNext(msg);
        }

#if LOGGING_TRUE
        protected DisposingLogger()
        {
            Log($"Created: {GetType()}");
        }
#endif
#region IDisposable

        protected virtual void ReleaseUnmanagedResources() { }

        [DebuggerStepThrough]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                Log($"Deleted: {GetType()}");
#if LOGGING_TRUE
            else
                Log($"Disposed: {GetType()}");
#endif

            lock (disposeLock)
            {
                if (Disposed) return;
                if (!disposing)
                    try
                    {
#if LOGGING_TRUE
                        Log("Trying to Release resources although we are not disposing!");
#endif
                        ReleaseUnmanagedResources();
                    }
                    catch (Exception e)
                    {
                        Log("Error releasing unmanaged resources: " + e);
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
