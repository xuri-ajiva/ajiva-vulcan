using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Models.Buffer;

public class ABuffer : DisposingLogger
{
    public ABuffer(uint size)
    {
        Size = size;
    }

    public SharpVk.Buffer? Buffer { get; private set; }
    public DeviceMemory? Memory { get; private set; }
    public uint Size { get; protected set; }

    public void Create(DeviceSystem system, BufferUsageFlags usage, MemoryPropertyFlags flags)
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
        ATrace.Assert(Memory != null, nameof(Memory) + " != null");
        return Memory.Map(0, Size, MemoryMapFlags.None);
    }

    public void Unmap()
    {
        Memory?.Unmap();
    }

    public DisposablePointer MapDisposer()
    {
        return new DisposablePointer(Memory!, Size);
    }

    public class DisposablePointer : IDisposable
    {
        private readonly DeviceMemory memory;

        public DisposablePointer(DeviceMemory memory, ulong size)
        {
            this.memory = memory;
            Ptr = memory.Map(0, size, MemoryMapFlags.None);
        }

        public IntPtr Ptr { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            memory.Unmap();
            GC.SuppressFinalize(this);
        }

        public unsafe void* ToPointer()
        {
            return Ptr.ToPointer();
        }
    }
}