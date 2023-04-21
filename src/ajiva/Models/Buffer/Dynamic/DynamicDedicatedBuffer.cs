using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.utils.Changing;
using SharpVk;

namespace ajiva.Models.Buffer.Dynamic;

public class ResizableDedicatedBuffer : DisposingLogger, IEquatable<ResizableDedicatedBuffer>
{
    private readonly IDeviceSystem system;
    private readonly BufferUsageFlags usage;
    private readonly MemoryPropertyFlags flags;

    public IChangingObserver<ResizableDedicatedBuffer> BufferResized { get; }

    public ResizableDedicatedBuffer(uint size, IDeviceSystem system, BufferUsageFlags usage, MemoryPropertyFlags flags)
    {
        BufferResized = new ChangingObserver<ResizableDedicatedBuffer>(this);
        Buffer = new Reactive<ABuffer>(null);
        Size = size;
        this.system = system;
        this.usage = usage;
        this.flags = flags;
        Resize(Size);
        system.WatchObject(this);
    }

    public Reactive<ABuffer> Buffer { get; }
    public uint Size { get; protected set; }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Buffer.Value?.Dispose();
    }

    public void Resize(uint newSize)
    {
        lock (this)
        {
            Size = newSize;
            //system.ToBeDeleted(Buffer.Value);  // let gc decide when to delete the buffer
            if (newSize == 0)
            {
                BufferResized.Changed();
                return;
            }

            Buffer.Value = ABuffer.Create(system, newSize, usage, flags);
            BufferResized.Changed();
        }
    }

    public ABuffer.DisposablePointer MapDisposer()
    {
        lock (this)
        {
            return Buffer.Value!.MapDisposer();
        }
    }


    public void CopyToRegions(ResizableDedicatedBuffer destination, ArrayProxy<BufferCopy> regions)
    {
        if (destination.Size < Size) throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(destination));

        system.QueueSingleTimeCommand(QueueType.TransferQueue, CommandPoolSelector.Transit, command =>
        {
            command.CopyBuffer(Current().Buffer, destination.Current().Buffer, regions);
        });
    }

    public ABuffer Current() => Buffer.Value!;

    /// <inheritdoc />
    public bool Equals(ResizableDedicatedBuffer? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return usage == other.usage && Size == other.Size && flags == other.flags && Buffer.Equals(other.Buffer) ;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return !ReferenceEquals(null, obj)
               && (ReferenceEquals(this, obj)
                   || obj.GetType() == GetType()
                   && Equals((ResizableDedicatedBuffer)obj));
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine((int)usage, (int)flags, Buffer, Size);
    }
}
