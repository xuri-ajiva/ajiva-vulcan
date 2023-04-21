using System.Diagnostics;

namespace Ajiva.Utils;

public interface IDisposingLogger : IDisposable
{
    public bool Disposed { get; }

    [DebuggerStepThrough]
    public void DisposeIn(int delayMs);

    /// <inheritdoc />
    [DebuggerStepThrough]
    abstract void IDisposable.Dispose();
}