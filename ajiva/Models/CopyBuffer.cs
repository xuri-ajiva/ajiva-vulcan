using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public class CopyBuffer<T> : BufferOfT<T> where T : notnull
    {
        /// <inheritdoc />
        public CopyBuffer(T[] val) : base(val)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void CopyValueToBuffer()
        {
            Throw.Assert(Memory != null, nameof(Memory) + " != null");
            var memPtr = Memory.Map(0, Size, MemoryMapFlags.None);

            for (var index = 0; index < Value.Length; index++)
            {
                Marshal.StructureToPtr(Value[index], memPtr + Unsafe.SizeOf<T>() * index, false);
            }

            Memory.Unmap();
        }

        public static CopyBuffer<T> CreateCopyBufferOnDevice(T[] val, DeviceManager manager)
        {
            var copyBuffer = new CopyBuffer<T>(val);

            copyBuffer.Create(manager.Device, BufferUsageFlags.TransferSource, typeFilter => manager.FindMemoryType(typeFilter, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent));
            copyBuffer.CopyValueToBuffer();
            return copyBuffer;
        }

        public void CopyTo(BufferOfT<T> aBuffer, DeviceManager manager)
        {
            var transferBuffers = manager.Device.AllocateCommandBuffer(manager.TransientCommandPool, CommandBufferLevel.Primary);

            transferBuffers.Begin(CommandBufferUsageFlags.OneTimeSubmit);

            if (aBuffer.Size < Size) throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(aBuffer));

            transferBuffers.CopyBuffer(Buffer, aBuffer.Buffer, new BufferCopy
            {
                Size = Size
            });

            transferBuffers.End();

            manager.TransferQueue.Submit(new SubmitInfo
            {
                CommandBuffers = new[]
                {
                    transferBuffers
                }
            }, null);
            manager.TransferQueue.WaitIdle();

            manager.TransientCommandPool.FreeCommandBuffers(transferBuffers);
        }
    }
}
