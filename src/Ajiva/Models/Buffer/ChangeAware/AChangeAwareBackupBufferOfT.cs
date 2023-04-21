using System.Collections;
using System.Runtime.CompilerServices;
using Ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace Ajiva.Models.Buffer.ChangeAware;

public class AChangeAwareBackupBufferOfT<T> : DisposingLogger, IAChangeAwareBackupBufferOfT<T> where T : unmanaged
{
    private int currentMax;

    public AChangeAwareBackupBufferOfT(int length, IDeviceSystem deviceSystem, BufferUsageFlags usageFlags = BufferUsageFlags.UniformBuffer)
    {
        Length = length;
        Changed = new BitArray(length);
        Value = new ByRef<T>[length];

        for (var i = 0; i < length; i++)
            Value[i] = new ByRef<T>(new T());

        SizeOfT = Unsafe.SizeOf<T>();

        this.deviceSystem = deviceSystem;
        currentMax = 0;

        Staging = new ABuffer((uint)(Length * SizeOfT));
        Uniform = new ABuffer((uint)(Length * SizeOfT));

        Staging.Create(deviceSystem, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
        Uniform.Create(deviceSystem, BufferUsageFlags.TransferDestination | usageFlags, MemoryPropertyFlags.DeviceLocal);
    }

    private IDeviceSystem deviceSystem { get; }

    public ByRef<T> this[in int index]
    {
        get => Value[index];
        set => Set(index, value);
    }

    /// <inheritdoc />
    public int Length { get; }

    /// <inheritdoc />
    public int SizeOfT { get; }

    /// <inheritdoc />
    public ABuffer Uniform { get; }

    /// <inheritdoc />
    public ABuffer Staging { get; }

    /// <inheritdoc />
    public ByRef<T>[] Value { get; }

    /// <inheritdoc />
    public BitArray Changed { get; }

    /// <inheritdoc />
    public void Set(int index, T value)
    {
        CheckBounds(index);
        Value[index] = value;
        Changed[index] = true;
    }

    /// <inheritdoc />
    public void Set(int index, ByRef<T> value)
    {
        CheckBounds(index);
        Value[index] = value;
        Changed[index] = true;
    }

    /// <inheritdoc />
    public ByRef<T> Get(int index)
    {
        CheckBounds(index);
        return Value[index];
    }

    /// <inheritdoc />
    public void CommitChanges()
    {
        var memPtr = Staging.MapDisposer();
        var simple = new List<Regions>();

        var cur = new Regions();

        for (var i = 0; i <= currentMax; i++) // go throw all known values
        {
            if (!Changed[i]) continue; // skip if not changed

            CopyValue(i, memPtr.Ptr); // update value in Staging buffer

            if (i - cur.End > cur.Length) // check if last region is close enough
            {
                simple.Add(cur);
                cur = new Regions(i, i);
            }
            else
            {
                cur.Extend(i);
            }
        }

        simple.Add(cur);

        var regions = simple
            .Where(x => x.Length > 0)
            .Select(x => new BufferCopy {
                Size = (ulong)(SizeOfT * x.Length),
                DestinationOffset = (ulong)(SizeOfT * x.Begin),
                SourceOffset = (ulong)(SizeOfT * x.Begin)
            }).ToArray();

        memPtr.Dispose(); // free address space

        Staging.CopyRegions(Uniform, regions, deviceSystem);

        Changed.SetAll(false);
    }

    /// <inheritdoc />
    public void Commit(int index)
    {
        var memPtr = Staging.MapDisposer();
        CopyValue(index, memPtr.Ptr);
        memPtr.Dispose();
        Staging.CopyRegions(Uniform, GetRegion(index), deviceSystem);
    }

    /// <inheritdoc />
    public void SetChanged(int index, bool changed)
    {
        Changed[index] = changed;
    }

    /// <inheritdoc />
    public ByRef<T> GetForChange(int index)
    {
        CheckBounds(index);
        Changed[index] = true;
        return Value[index];
    }

    private void CopyValue(int index, IntPtr ptr)
    {
        unsafe
        {
            fixed (T* src = &Value[index].Value)
            {
                *(T*)(ptr + SizeOfT * index) = *src;
            }
        }
        //Marshal.StructureToPtr(Value[index].Value, ptr + SizeOfT * index, true);
    }

    private BufferCopy GetRegion(int index)
    {
        return new BufferCopy {
            Size = (ulong)SizeOfT,
            DestinationOffset = (ulong)(SizeOfT * index),
            SourceOffset = (ulong)(SizeOfT * index)
        };
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Staging.Dispose();
        Uniform.Dispose();
    }

    private void CheckBounds(int index)
    {
        if (index > currentMax)
        {
            if (index > Length)
                //todo resize array if to small
                throw new IndexOutOfRangeException("Currently not resizable!");
            currentMax = index;
        }
    }

    private struct Regions
    {
        public Regions(int begin, int end)
        {
            Begin = begin;
            End = end;
            initialized = true;
        }

        public int Begin;
        public int End;
        private bool initialized;
        public int Length => End - Begin + 1;

        public void Extend(int end)
        {
            if (!initialized)
            {
                Begin = end;
                initialized = true;
            }
            End = end;
        }
    }
}