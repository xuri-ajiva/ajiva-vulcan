using ajiva.Systems.VulcanEngine.Interfaces;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Models.Buffer.ChangeAware;

public static class StaticBufferExtensions
{
    public static void SetAndCommit<T>(this IAChangeAwareBufferOfT<T> changeAwareBufferOfT, int index, T value) where T : struct
    {
        changeAwareBufferOfT.Set(index, value);
        changeAwareBufferOfT.Commit(index);
    }

    public static void SetAndCommit<T>(this IAChangeAwareBackupBufferOfT<T> changeAwareBufferOfT, int index, T value) where T : unmanaged
    {
        changeAwareBufferOfT.Set(index, value);
        changeAwareBufferOfT.Commit(index);
    }

    public static void CopyTo<T>(this IAChangeAwareBufferOfT<T> from, IAChangeAwareBufferOfT<T> to, IDeviceSystem system) where T : struct
    {
        from.Buffer.CopyTo(to.Buffer, system);
    }

    public static void CopyTo<T>(this IAChangeAwareBufferOfT<T> from, ABuffer to, IDeviceSystem system) where T : struct
    {
        from.Buffer.CopyTo(to, system);
    }

    public static void CopyTo(this ABuffer from, ABuffer to, IDeviceSystem system)
    {
        if (from.Size > to.Size)
            throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(to));

        system.QueueSingleTimeCommand(QueueType.TransferQueue, CommandPoolSelector.Transit, command =>
        {
            command.CopyBuffer(from.Buffer, to.Buffer, new BufferCopy
            {
                Size = from.Size
            });
        });
    }

    public static void CopyRegions(this ABuffer from, ABuffer to, ArrayProxy<BufferCopy> regions, IDeviceSystem system)
    {
        if (from.Size > to.Size)
            throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(to));

        system.QueueSingleTimeCommand(QueueType.TransferQueue, CommandPoolSelector.Transit, command =>
        {
            command.CopyBuffer(from.Buffer, to.Buffer, regions);
        });
    }
}
