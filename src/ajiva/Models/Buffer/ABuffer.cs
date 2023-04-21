using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace ajiva.Models.Buffer;

public class ABuffer : DisposingLogger, IEquatable<ABuffer>
{
    static object _lock = new object();

    public ABuffer(uint size)
    {
        Size = size;
    }

    public SharpVk.Buffer? Buffer { get; private set; }
    public DeviceMemory? Memory { get; private set; }
    public uint Size { get; protected set; }

    public void Create(IDeviceSystem system, BufferUsageFlags usage, MemoryPropertyFlags flags)
    {
        Buffer = system.Device!.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

        var memRequirements = Buffer.GetMemoryRequirements();

        Memory = system.Device.AllocateMemory(memRequirements.Size, system.FindMemoryType(memRequirements.MemoryTypeBits, flags));
        Buffer.BindMemory(Memory, 0);
        system.WatchObject(this);
    }    
    public static ABuffer Create(IDeviceSystem system, uint size, BufferUsageFlags usage, MemoryPropertyFlags flags)
    {
        var res = new ABuffer(size);
        res.Create(system, usage, flags);
        return res;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Log.Verbose("Buffer Deleted: {RawHandle}, Disposing: {disposing}",Buffer.RawHandle, disposing);
        Buffer?.Dispose();
        Memory?.Free();
    }

    public IntPtr Map()
    {
        lock (_lock)
        {
            ATrace.Assert(Memory != null, nameof(Memory) + " != null");
            return Memory.Map(0, Size, MemoryMapFlags.None);
        }
    }

    public void Unmap()
    {
        lock (_lock)
        {
            Memory?.Unmap();
        }
    }

    public DisposablePointer MapDisposer()
    {
        return new DisposablePointer(this, Size);
    }

    public class DisposablePointer : IDisposable
    {
        private readonly ABuffer buffer;

        public DisposablePointer(ABuffer buffer, ulong size)
        {
            this.buffer = buffer;
            ptr = new Lazy<IntPtr>(buffer.Map);
        }

        private Lazy<IntPtr> ptr;
        public IntPtr Ptr => ptr.Value;

        /// <inheritdoc />
        public void Dispose()
        {
            if (ptr.IsValueCreated)
            {
                ptr = null!;
                buffer.Unmap();
            }
            GC.SuppressFinalize(this);
        }

        public unsafe void* ToPointer()
        {
            return Ptr.ToPointer();
        }
    }

    /// <inheritdoc />
    public bool Equals(ABuffer? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Buffer, other.Buffer) && Equals(Memory, other.Memory) && Size == other.Size;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ABuffer)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Buffer, Memory, Size);
    }
}
