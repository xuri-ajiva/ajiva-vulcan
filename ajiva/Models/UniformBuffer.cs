using System;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public class UniformBuffer<T> : DisposingLogger where T : struct
    {
        private readonly DeviceComponent component;
        private const int ItemCount = 1;
        public WritableCopyBuffer<T> Staging { get; }
        public BufferOfT<T> Uniform { get; }

        public UniformBuffer(DeviceComponent component)
        {
            var value = new T[ItemCount];

            this.component = component;

            Staging = new(value);
            Uniform = new(value);
        }

        public void Create()
        {
            Staging.Create(component, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            Uniform.Create(component, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal);
        }

        public void Update(T[] toUpdate)
        {
            Staging.Update(toUpdate);
        }

        public void Copy()
        {
            Staging.CopyTo(Uniform, component);
        }

        public void UpdateCopyOne(T data, uint id)
        {
            Uniform.Value[id] = data;
            Staging.CopyRegions(Uniform, new BufferCopy
            {
                Size = Uniform.SizeOfT,
                DestinationOffset = Uniform.SizeOfT * id,
                SourceOffset = Uniform.SizeOfT * id
            }, component);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Staging.Dispose();
            Uniform.Dispose();
        }
    }
}
