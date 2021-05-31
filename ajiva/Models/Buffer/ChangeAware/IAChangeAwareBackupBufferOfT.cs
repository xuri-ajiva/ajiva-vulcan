using System;
using System.Collections;

namespace ajiva.Models.Buffer.ChangeAware
{
    public interface IAChangeAwareBackupBufferOfT<T> : IDisposable where T : struct
    {
        T this[in int index] { get; set; }

        int Length { get; }
        int SizeOfT { get; }
        ABuffer Uniform { get; }
        ABuffer Staging { get; }
        T[] Value { get; }
        BitArray Changed { get; }
        void Set(int index, T value);
        void CommitChanges();
        void Commit(int index);
        ref T GetRef(int index);
        void SetChanged(int index, bool changed);
    }
}
