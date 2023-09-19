using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ajiva.Models;

public abstract class ThreadSaveCreatable : DisposingLogger
{
    public bool Created { get; protected set; }

    [MethodImpl(MethodImplOptions.Synchronized)]
    [DebuggerStepThrough]
    public void EnsureExists()
    {
        if (Created) return;
        Create();
        Created = true;
    }

    protected abstract void Create();
}