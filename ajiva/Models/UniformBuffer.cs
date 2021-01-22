using ajiva.Helpers;
using ajiva.Systems.VulcanEngine.EngineManagers;
using SharpVk;

namespace ajiva.Models
{
    public class UniformBuffer<T> : DisposingLogger, IThreadSaveCreatable where T : struct
    {
        public bool Created { get; private set; }

        private readonly DeviceComponent component;
        public WritableCopyBuffer<T> Staging { get; }
        public BufferOfT<T> Uniform { get; }

        public UniformBuffer(DeviceComponent component, int itemCount)
        {
            var value = new T[itemCount];

            this.component = component;

            Staging = new(value);
            Uniform = new(value);
        }

        public void EnsureExists()
        {
            if (Created) return;
            Staging.Create(component, BufferUsageFlags.TransferSource, MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);
            Uniform.Create(component, BufferUsageFlags.TransferDestination | BufferUsageFlags.UniformBuffer, MemoryPropertyFlags.DeviceLocal);
            Created = true;
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

        public void UpdateOne(T data, uint id)
        {
            Uniform.Value[id] = data;
        }

        public delegate void BufferValueUpdateDelegate(int index, ref T value);

        public void UpdateExpresion(BufferValueUpdateDelegate updateFunc)
        {
            for (int i = 0; i < Staging.Length; i++)
            {
                updateFunc(i, ref Staging.Value[i]);
            }
            Staging.CopyValueToBuffer();
        }
    }
}
