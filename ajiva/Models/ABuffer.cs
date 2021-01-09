using System;
using System.Runtime.CompilerServices;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Models
{
    public abstract class ABuffer : IDisposable
    {
        public Buffer? Buffer;
        public DeviceMemory? Memory;
        public readonly uint Size;

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

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Buffer?.Dispose();
            Memory?.Free();
            GC.SuppressFinalize(this);
        }
    }
    public class BufferOfT<T> : ABuffer where T : notnull
    {
        public int Length { get; }
        public uint SizeOfT { get; }
        public T[] Value { get; protected set; }

        /// <inheritdoc />
        public BufferOfT(T[] val) : base((uint)(Unsafe.SizeOf<T>() * val.Length))
        {
            Value = val;
            Length = val.Length;
            SizeOfT = (uint)Unsafe.SizeOf<T>();
        }

        public void ClearT()
        {
            Value = Array.Empty<T>();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            ClearT();
        }
    }
}
