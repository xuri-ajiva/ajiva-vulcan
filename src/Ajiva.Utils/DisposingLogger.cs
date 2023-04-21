#define LOGGING_TRUE
using System.Diagnostics;

namespace Ajiva.Utils;

public abstract class DisposingLogger : IDisposingLogger
{
    protected readonly object DisposeLock = new object();

#if LOGGING_TRUE
    protected DisposingLogger()
    {
        Log($"Created: {GetType()}");
    }
#endif
    public bool Disposed { get; private set; }

    [DebuggerStepThrough]
    private static void Log(string msg)
    {
        //LogHelper.Log(msg);
        //Block.WriteNext(msg);
    }

#region IDisposable

    protected virtual void ReleaseUnmanagedResources(bool disposing)
    {
    }

    [DebuggerStepThrough]
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            Log($"Deleted: {GetType()}");
#if LOGGING_TRUE
        else
            Log($"Disposed: {GetType()}");
#endif

        lock (DisposeLock)
        {
            if (Disposed) return;
            if (!disposing)
                try
                {
#if LOGGING_TRUE
                    Log("Trying to Release resources although we are not disposing!");
#endif
                    ReleaseUnmanagedResources(false);
                }
                catch (Exception e)
                {
                    Log("Error releasing unmanaged resources: " + e);
                }
            else
                ReleaseUnmanagedResources(true);
            Disposed = true;
        }
    }

    [DebuggerStepThrough]
    public void DisposeIn(int delayMs)
    {
        GC.SuppressFinalize(this);
        Task.Run(async () => //todo was TaskWatcher
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