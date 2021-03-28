using System;
using System.Diagnostics;

namespace ajiva.Utils
{
    public interface IDisposingLogger : IDisposable
    {
        bool Disposed { get; }

        [DebuggerStepThrough]
        void DisposeIn(int delayMs);

        /// <inheritdoc />
        [DebuggerStepThrough]
        abstract void IDisposable.Dispose();
    }
}