using System.Collections.Generic;
using System.Linq;
using ajiva.Systems.VulcanEngine.Systems;
using SharpVk;

namespace ajiva.Models.Buffer
{
    public class UniformBuffer<T> : ThreadSaveCreatable where T : struct, IComp<T>
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
            Staging[id] = data;
            Staging.CopyRegions(Uniform, GetRegion(id), system);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            Staging.Dispose();
            Uniform.Dispose();
        }

        public void UpdateOne(T data, uint id)
        {
            Uniform[id] = data;
        }

        public delegate bool BufferValueUpdateDelegate(uint index, ref T value);

        public void UpdateExpresion(BufferValueUpdateDelegate updateFunc)
        {
            List<uint> updated = new();
            for (uint i = 0; i < Staging.Length; i++)
            {
                if (updateFunc(i, ref Staging.GetRef(i)))
                    updated.Add(i);
            }
            CopyRegions(updated);
        }

        public void UpdateExpresionOne(uint i, BufferValueUpdateDelegate updateFunc)
        {
            if (updateFunc(i, ref Staging.GetRef(i)))
                Staging.CopyRegions(Uniform, GetRegion(i), system);
        }

        public void CopyRegions(List<uint> updated)
        {
            //todo simplify regions, e.g join neighbors
            Staging.CopySetValueToBuffer(updated);
            Staging.CopyRegions(Uniform, SimplifyFyRegions(updated), system);
        }

        private BufferCopy[] RegionsToBufferCopy(List<uint> updated)
            //stupid idea
            => updated.Select(GetRegion).ToArray();
        private BufferCopy[] SimplifyFyRegions(List<uint> updated)
        {
            updated.Sort();

            List<Regions> simple = new();

            Regions cur = new();
            foreach (var u in updated)
            {
                if (u - cur.End > cur.Length)
                {
                    simple.Add(cur);
                    cur = new(u, u);
                }
                else
                {
                    cur.Extend(u);
                }
            }

            simple.Add(cur);
            
            return simple
                .Where(x => x.Length > 0)
                .Select(x => new BufferCopy
                {
                    Size = Uniform.SizeOfT * x.Length,
                    DestinationOffset = Uniform.SizeOfT * x.Begin,
                    SourceOffset = Uniform.SizeOfT * x.Begin,
                }).ToArray();
        }
        

        private struct Regions
        {
            public Regions(uint begin, uint end)
            {
                Begin = begin;
                End = end;
                initialized = true;
            }

            public uint Begin;
            public uint End;
            private bool initialized;
            public uint Length => End - Begin + 1;

            public void Extend(uint end)
            {
                if (!initialized)
                {
                    Begin = end;
                    initialized = true;
                }
                End = end;
            }
        }
        private BufferCopy GetRegion(uint id) => new() {Size = Uniform.SizeOfT, DestinationOffset = Uniform.SizeOfT * id, SourceOffset = Uniform.SizeOfT * id};
    }
}
