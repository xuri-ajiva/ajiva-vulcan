using System.Runtime.CompilerServices;

namespace ajiva.Models
{
    public interface IThreadSaveCreatable
    {
        bool Created { get; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public abstract void EnsureExists();
    }
}
