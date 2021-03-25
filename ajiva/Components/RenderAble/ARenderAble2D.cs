using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using SharpVk;

namespace ajiva.Components.RenderAble
{
    public class ARenderAble2D : ARenderAble
    {
        public Mesh<Vertex2D>? Mesh { get; private set; }

        public ARenderAble2D()
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
        protected override void ReleaseUnmanagedResources()
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
