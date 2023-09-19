using System.Diagnostics;

namespace Ajiva.Utils;

public interface IDisposingLogger : IDisposable
{
    public bool Disposed { get; }

    /// <inheritdoc />
    [DebuggerStepThrough]
    abstract void IDisposable.Dispose();

    [DebuggerStepThrough]
    public void DisposeIn(int delayMs);
}