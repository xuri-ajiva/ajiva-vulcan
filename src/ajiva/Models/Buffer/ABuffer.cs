using ajiva.Systems.VulcanEngine.Interfaces;
using SharpVk;

namespace ajiva.Models.Buffer;

public class ABuffer : DisposingLogger
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
        //todo: system.EnsureDevicesExist();

        Buffer = system.Device!.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

        var memRequirements = Buffer.GetMemoryRequirements();

        Memory = system.Device.AllocateMemory(memRequirements.Size, system.FindMemoryType(memRequirements.MemoryTypeBits, flags));
        Buffer.BindMemory(Memory, 0);
        system.WatchObject(this);
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
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
            Ptr = buffer.Map();
        }

        public IntPtr Ptr { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            buffer.Unmap();
            GC.SuppressFinalize(this);
        }

        public unsafe void* ToPointer()
        {
            return Ptr.ToPointer();
        }
    }
}
