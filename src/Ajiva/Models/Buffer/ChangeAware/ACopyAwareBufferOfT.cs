using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace Ajiva.Models.Buffer.ChangeAware;

public class AChangeAwareBufferOfT<T> : DisposingLogger, IAChangeAwareBufferOfT<T> where T : struct
{
    private int currentMax;

    public AChangeAwareBufferOfT(int length, DeviceSystem deviceSystem, BufferUsageFlags usage, MemoryPropertyFlags flags)
    {
        Length = length;
        Changed = new BitArray(length);
        Value = new T[length];
        SizeOfT = Unsafe.SizeOf<T>();
        Buffer = new ABuffer((uint)(SizeOfT * length));
        Buffer.Create(deviceSystem, usage, flags);
    }

    public AChangeAwareBufferOfT(T[] value, DeviceSystem deviceSystem, BufferUsageFlags usage, MemoryPropertyFlags flags)
    {
        Length = value.Length;
        Changed = new BitArray(value.Length);
        Value = value;
        SizeOfT = Unsafe.SizeOf<T>();
        Buffer = new ABuffer((uint)(SizeOfT * Length));
        Buffer.Create(deviceSystem, usage, flags);
    }

    /// <inheritdoc />
    public int Length { get; }

    /// <inheritdoc />
    public int SizeOfT { get; }

    /// <inheritdoc />
    public ABuffer Buffer { get; }

    /// <inheritdoc />
    public T[] Value { get; }

    /// <inheritdoc />
    public BitArray Changed { get; }

    /// <inheritdoc />
    public void Set(int index, T value)
    {
        if (index > currentMax)
        {
            if (index > Length)
                //todo resize array if to small
                throw new IndexOutOfRangeException("Currently not resizable!");
            currentMax = index;
        }
        Value[index] = value;
        Changed[index] = true;
    }

    /// <inheritdoc />
    public void CommitChanges()
    {
        using var memPtr = Buffer.MapDisposer();
        for (var i = 0; i < currentMax; i++)
            if (Changed[i])
                Marshal.StructureToPtr(Value[i], memPtr.Ptr + SizeOfT * i, true);
        memPtr.Dispose();
        Changed.SetAll(false);
    }

    /// <inheritdoc />
    public void Commit(int index)
    {
        using var memPtr = Buffer.MapDisposer();
        Marshal.StructureToPtr(Value[index], memPtr.Ptr + SizeOfT * index, true);
        memPtr.Dispose();
    }

    /// <inheritdoc />
    public ref T GetRef(int index)
    {
        return ref Value[index];
    }

    /// <inheritdoc />
    public void SetChanged(int index, bool changed)
    {
        Changed[index] = changed;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Buffer.Dispose();
    }
}