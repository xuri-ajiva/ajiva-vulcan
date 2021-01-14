//#define LOGGING_TRUE
using System;

namespace ajiva.Engine
{
    public abstract class DisposingLogger : IDisposable
    {
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
            ReleaseUnmanagedResources();
            if (disposing)
            {
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
