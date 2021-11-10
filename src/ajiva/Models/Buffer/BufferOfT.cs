using System.Runtime.CompilerServices;

namespace ajiva.Models.Buffer;

public class BufferOfT<T> : ABuffer, IBufferOfT where T : struct
{
    /// <inheritdoc />
    public BufferOfT(T[] val) : base((uint)(Unsafe.SizeOf<T>() * val.Length))
    {
        Value = val;
        Length = val.Length;
        SizeOfT = (uint)Unsafe.SizeOf<T>();
    }

    protected T[] Value { get; set; }

    public T this[in uint index]
    {
        get => Value[index];
        set => Value[index] = value;
    }
    public T this[in int index]
    {
        get => Value[index];
        set => Value[index] = value;
    }
    public int Length { get; }
    public uint SizeOfT { get; }

    public void ClearT()
    {
        Value = null!;
    }

    public ref T GetRef(int index)
    {
        if (index > Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "");
        return ref Value[index];
    }

    public ref T GetRef(uint index)
    {
        if (index > Length)
            throw new ArgumentOutOfRangeException(nameof(index), index, "");
        return ref Value[index];
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        ClearT();
        base.ReleaseUnmanagedResources(disposing);
    }
}