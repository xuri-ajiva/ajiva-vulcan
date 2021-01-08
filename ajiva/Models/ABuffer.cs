using System;
using System.Runtime.CompilerServices;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Models
{
    public class ABuffer : IDisposable
    {
        public Buffer? Buffer;
        public DeviceMemory? Memory;
        public readonly uint Size;

        /// <inheritdoc />
        public void Dispose()
        {
            Buffer?.Dispose();
            Memory?.Free();
            GC.SuppressFinalize(this);
        }

        public ABuffer(uint size)
        {
            Size = size;
        }

        public delegate uint MemoryTypeIndexDelegate(uint typeFilter);

        public void Create(Device device, BufferUsageFlags usage, MemoryTypeIndexDelegate memoryTypeIndex)
        {
            Buffer = device.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

            var memRequirements = Buffer.GetMemoryRequirements();

            Memory = device.AllocateMemory(memRequirements.Size, memoryTypeIndex(memRequirements.MemoryTypeBits));
            Buffer.BindMemory(Memory, 0);
        }
    }
    public class BufferOfT<T> : ABuffer where T : notnull
    {
        public int Length { get; }
        public T[] Value { get; protected set; }

        /// <inheritdoc />
        public BufferOfT(T[] val) : base((uint)(Unsafe.SizeOf<T>() * val.Length))
        {
            Value = val;
            Length = val.Length;
        }

        public void ClearT()
        {
            Value = Array.Empty<T>();
        }
    }
}
