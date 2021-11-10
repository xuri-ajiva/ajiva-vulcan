using System.Collections;

namespace ajiva.Models.Buffer.ChangeAware;

public interface IAChangeAwareBackupBufferOfT<T> : IDisposable where T : unmanaged
{
    ByRef<T> this[in int index] { get; set; }

    int Length { get; }
    int SizeOfT { get; }
    ABuffer Uniform { get; }
    ABuffer Staging { get; }
    ByRef<T>[] Value { get; }
    BitArray Changed { get; }
    void Set(int index, T value);
    ByRef<T> Get(int index);
    void CommitChanges();
    void Commit(int index);
    void SetChanged(int index, bool changed);
    ByRef<T> GetForChange(int index);
}