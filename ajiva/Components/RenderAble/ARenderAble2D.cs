using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using ajiva.Utils.Changing;
using SharpVk;

namespace ajiva.Components.RenderAble
{
    public class ARenderAble2D : ARenderAble
    {
        private Mesh<Vertex2D>? mesh;
        public Mesh<Vertex2D>? Mesh
        {
            get => mesh;
            private set => ChangingObserver.RaiseChanged(ref mesh, value);
        }

        public ARenderAble2D() : base(ChangingCacheMode.DirectUpdate)
        {
            Render = false;
            Id = INextId<ARenderAble2D>.Next();
        }

        public void SetMesh(Mesh<Vertex2D>? mesh, DeviceSystem system)
        {
            Mesh?.Dispose();
            Mesh = mesh;
            Mesh?.Create(system);
            Render &= mesh != null;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            INextId<ARenderAble2D>.Remove(Id);
            Mesh?.Dispose();
        }

        /// <inheritdoc />
        public override void BindAndDraw(CommandBuffer commandBuffer)
        {
            Mesh?.Bind(commandBuffer);

            Mesh?.DrawIndexed(commandBuffer);
        }

        /// <inheritdoc />
        public override AjivaEngineLayer AjivaEngineLayer { get; } = AjivaEngineLayer.Layer2d;
    }
}
