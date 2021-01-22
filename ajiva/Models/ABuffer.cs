using System;
using ajiva.Helpers;
using ajiva.Systems.RenderEngine.EngineManagers;
using SharpVk;
using Buffer = SharpVk.Buffer;

namespace ajiva.Models
{
    public class ABuffer : DisposingLogger
    {
        public Buffer? Buffer;
        public DeviceMemory? Memory;
        public uint Size { get; }

        public ABuffer(uint size)
        {
            Size = size;
        }

        public void Create(DeviceComponent component, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            component.EnsureDevicesExist();
            
            Buffer = component.Device!.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

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
}
