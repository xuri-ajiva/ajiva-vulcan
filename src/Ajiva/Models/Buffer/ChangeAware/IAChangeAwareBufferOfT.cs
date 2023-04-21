using System.Collections;

namespace Ajiva.Models.Buffer.ChangeAware;

public interface IAChangeAwareBufferOfT<T> : IDisposable where T : struct
{
    int Length { get; }
    int SizeOfT { get; }
    ABuffer Buffer { get; }
    T[] Value { get; }
    BitArray Changed { get; }
    void Set(int index, T value);
    void CommitChanges();
    void Commit(int index);
    ref T GetRef(int index);
    void SetChanged(int index, bool changed);
}