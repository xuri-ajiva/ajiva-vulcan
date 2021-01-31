using ajiva.Ecs.Component;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.EngineManagers;
using ajiva.Systems.VulcanEngine.Systems;

namespace ajiva.Components
{
    public class ARenderAble : DisposingLogger, IComponent
    {
        public uint Id { get; }
        public Mesh? Mesh { get; private set; }
        public bool Render { get; set; }

        public ARenderAble()
        {
            Render = false;
            Id = INextId<ARenderAble>.Next();
        }
        
        public void SetMesh(Mesh? mesh, DeviceSystem system)
        {
            Mesh?.Dispose();
            Mesh = mesh;
            Mesh?.Create(system);
            Render &= mesh != null;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            INextId<ARenderAble>.Remove(Id);
            Mesh?.Dispose();
        }


        /// <inheritdoc />
        public bool Dirty { get; set; }
    }
}
