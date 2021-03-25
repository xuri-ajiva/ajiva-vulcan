using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Models.Buffer
{
    public class CopyBuffer<T> : BufferOfT<T> where T : struct
    {
        /// <inheritdoc />
        public CopyBuffer(T[] val) : base(val)
        {
        }

        public void CopyValueToBuffer()
        {
            ATrace.Assert(Memory != null, nameof(Memory) + " != null");
            var memPtr = Memory.Map(0, Size, MemoryMapFlags.None);

            for (var index = 0; index < Value.Length; index++)
            {
                Marshal.StructureToPtr(Value[index], memPtr + Unsafe.SizeOf<T>() * index, true);
            }

            Memory.Unmap();
        }

        public void CopySingleValueToBuffer(int id)
        {
            ATrace.Assert(Memory != null, nameof(Memory) + " != null");
            var memPtr = Memory.Map(0, Size, MemoryMapFlags.None);

            Marshal.StructureToPtr(Value[id], memPtr + Unsafe.SizeOf<T>() * id, true);

            Memory.Unmap();
        }

        public void CopySetValueToBuffer(IEnumerable<uint> ids)
        {
            ATrace.Assert(Memory != null, nameof(Memory) + " != null");
            var memPtr = Memory.Map(0, Size, MemoryMapFlags.None);

            foreach (var u in ids)
            {
                Marshal.StructureToPtr(Value[u], memPtr + (Unsafe.SizeOf<T>() * (int)u), true);
            }

            Memory.Unmap();
        }

        public static CopyBuffer<T> CreateCopyBufferOnDevice(T[] val, DeviceSystem system)
        {
            var copyBuffer = new CopyBuffer<T>(val);

            copyBuffer.Create(system, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            copyBuffer.CopyValueToBuffer();
            return copyBuffer;
        }

        public void CopyTo(BufferOfT<T> aBuffer, DeviceSystem system)
        {
            //todo: system.EnsureDevicesExist();

            if (aBuffer.Size < Size) throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(aBuffer));

            system.SingleTimeCommand(x => x.GraphicsQueue!, command =>
            {
                command.CopyBuffer(Buffer, aBuffer.Buffer, new BufferCopy
                {
                    Size = Size
                });
            });
        }

        public void CopyRegions(BufferOfT<T> aBuffer, ArrayProxy<BufferCopy> regions, DeviceSystem system)
        {
            //todo: system.EnsureDevicesExist();

            if (aBuffer.Size < Size) throw new ArgumentException("The Destination Buffer is smaller than the Source Buffer", nameof(aBuffer));

            system.SingleTimeCommand(x => x.GraphicsQueue!, command =>
            {
                command.CopyBuffer(Buffer, aBuffer.Buffer, regions);
            });
        }
    }
}
