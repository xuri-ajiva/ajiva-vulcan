using System.Diagnostics;

namespace ajiva.utils
{
    public interface IDisposingLogger : IDisposable
    {
        public bool Disposed { get; }

        [DebuggerStepThrough]
        public void DisposeIn(int delayMs);

        /// <inheritdoc />
        [DebuggerStepThrough]
        abstract void IDisposable.Dispose();
    }
}
