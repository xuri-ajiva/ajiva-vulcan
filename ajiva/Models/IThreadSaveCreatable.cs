using System.Runtime.CompilerServices;
using ajiva.Engine;

namespace ajiva.Models
{
    public interface IThreadSaveCreatable
    {
        bool Created { get; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public abstract void EnsureExists();
    }
}
