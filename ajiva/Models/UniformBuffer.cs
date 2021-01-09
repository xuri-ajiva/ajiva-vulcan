using System;
using System.Runtime.CompilerServices;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public class UniformBuffer<T> : IDisposable where T : struct
    {
        private readonly DeviceManager manager;
        private const int ItemCount = 1;
        public WritableCopyBuffer<T> Staging { get; }
        public BufferOfT<T> Uniform { get; }

        public UniformBuffer(DeviceManager manager)
        {
            var value = new T[ItemCount];

            this.manager = manager;

            Staging = new(value);
            Uniform = new(value); //Uniform = new((uint)Unsafe.SizeOf<T>() * ItemCount);

        }

        public void Create()
        {
            Staging.Create(manager, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            Uniform.Create(manager, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer,  MemoryPropertyFlags.DeviceLocal);
        }

        public void Update(T[] toUpdate)
        {
            Staging.Update(toUpdate);
        }

        public void Copy()
        {
            Staging.CopyTo(Uniform, manager);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Staging.Dispose();
            Uniform.Dispose();
            GC.SuppressFinalize(this);
        }

        public void UpdateCopyOne(T data, uint id)
        {
            Uniform.Value[id] = data;
            Staging.CopyRegions(Uniform, new BufferCopy
            {
                Size = Uniform.SizeOfT,
                DestinationOffset = Uniform.SizeOfT * id,
                SourceOffset = Uniform.SizeOfT * id
            }, manager);
        }
    }
}
