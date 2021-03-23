using System.Collections.Generic;
using System.Linq;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Models
{
    public class UniformBuffer<T> : ThreadSaveCreatable where T : struct
    {
        private readonly DeviceSystem system;
        public WritableCopyBuffer<T> Staging { get; }
        public BufferOfT<T> Uniform { get; }

        public UniformBuffer(DeviceSystem system, int itemCount)
        {
            var value = new T[itemCount];

            this.system = system;

            Staging = new(value);
            Uniform = new(value);
        }

        /// <inheritdoc />
        protected override void Create()
        {
            Staging.Create(system, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            Uniform.Create(system, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal);
        }

        public void Update(T[] toUpdate)
        {
            Staging.Update(toUpdate);
        }

        public void Copy()
        {
            Staging.CopyTo(Uniform, system);
        }

        public void UpdateCopyOne(T data, uint id)
        {
            Staging.Value[id] = data;
            Staging.CopyRegions(Uniform, GetRegion(id), system);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Staging.Dispose();
            Uniform.Dispose();
        }

        public void UpdateOne(T data, uint id)
        {
            Uniform.Value[id] = data;
        }

        public delegate bool BufferValueUpdateDelegate(uint index, ref T value);

        public void UpdateExpresion(BufferValueUpdateDelegate updateFunc)
        {
            List<uint> updated = new();
            for (uint i = 0; i < Staging.Length; i++)
            {
                if (updateFunc(i, ref Staging.Value[i]))
                    updated.Add(i);
            }
            CopyRegions(updated);
        }

        public void UpdateExpresionOne(uint i, BufferValueUpdateDelegate updateFunc)
        {
            if (updateFunc(i, ref Staging.Value[i]))
                Staging.CopyRegions(Uniform, GetRegion(i), system);
        }

        public void CopyRegions(List<uint> updated)
        {
            //todo simplify regions, e.g join neighbors
            Staging.CopySetValueToBuffer(updated);
            Staging.CopyRegions(Uniform, updated.Select(GetRegion).ToArray(), system);
        }

        private BufferCopy GetRegion(uint id) => new() {Size = Uniform.SizeOfT, DestinationOffset = Uniform.SizeOfT * id, SourceOffset = Uniform.SizeOfT * id};
    }
}
