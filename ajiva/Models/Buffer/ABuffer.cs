﻿using System;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Models.Buffer
{
    public class ABuffer : DisposingLogger
    {
        public SharpVk.Buffer? Buffer;
        public DeviceMemory? Memory;
        public uint Size { get; protected set; }

        public ABuffer(uint size)
        {
            Size = size;
        }

        public void Create(DeviceSystem system, BufferUsageFlags usage, MemoryPropertyFlags flags)
        {
            //todo: system.EnsureDevicesExist();
            
            Buffer = system.Device!.CreateBuffer(Size, usage, SharingMode.Exclusive, null);

            var memRequirements = Buffer.GetMemoryRequirements();

            Memory = system.Device.AllocateMemory(memRequirements.Size, system.FindMemoryType(memRequirements.MemoryTypeBits, flags));
            Buffer.BindMemory(Memory, 0);
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
