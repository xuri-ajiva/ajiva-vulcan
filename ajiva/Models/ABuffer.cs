using System;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Models
{
    public class ABuffer : DisposingLogger
    {
        public Buffer? Buffer;
        public DeviceMemory? Memory;
        public readonly uint Size;

        public ABuffer(uint size)
        {
            Size = size;
        }

        public void Create(DeviceComponent component, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            Buffer = component.Device.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

            var memRequirements = Buffer.GetMemoryRequirements();

            Memory = component.Device.AllocateMemory(memRequirements.Size, component.FindMemoryType(memRequirements.MemoryTypeBits, flags));
            Buffer.BindMemory(Memory, 0);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
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
                GC.SuppressFinalize(this);
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
        protected override void ReleaseUnmanagedResources()
        {
            ClearT();
            base.ReleaseUnmanagedResources();
        }
    }
}
