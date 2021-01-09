using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ajiva.EngineManagers;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Models
{
    public class ABuffer : IDisposable
    {
        public Buffer? Buffer;
        public DeviceMemory? Memory;
        public readonly uint Size;

        public ABuffer(uint size)
        {
            Size = size;
        }

        public delegate uint MemoryTypeIndexDelegate(uint typeFilter);

        public void Create(DeviceManager manager, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            Buffer = manager.Device.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

            var memRequirements = Buffer.GetMemoryRequirements();

            Memory = manager.Device.AllocateMemory(memRequirements.Size, manager.FindMemoryType(memRequirements.MemoryTypeBits, flags));
            Buffer.BindMemory(Memory, 0);
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Buffer?.Dispose();
            Memory?.Free();
            GC.SuppressFinalize(this);
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
            return new(Memory!, Size);
        }

        public class DisposablePointer : IDisposable
        {
            private readonly DeviceMemory memory;
            public IntPtr Ptr { get; }

            public DisposablePointer(DeviceMemory memory, ulong size)
            {
                this.memory = memory;
                Ptr = memory.Map(0, size, MemoryMapFlags.None);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                memory.Unmap();
            }

            public unsafe void* ToPointer()
            {
                return Ptr.ToPointer();
            }
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
