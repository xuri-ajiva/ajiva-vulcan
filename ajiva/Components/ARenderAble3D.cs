using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Systems;
using ajiva.Systems.VulcanEngine.Unions;
using SharpVk;

namespace ajiva.Components
{
    public class ARenderAble3D : ARenderAble
    {
        public Mesh<Vertex3D>? Mesh { get; private set; }

        public ARenderAble3D()
        {
            Render = false;
            Id = INextId<ARenderAble3D>.Next();
        }

        public void SetMesh(Mesh<Vertex3D>? mesh, DeviceSystem system)
        {
            Mesh?.Dispose();
            Mesh = mesh;
            Mesh?.Create(system);
            Render &= mesh != null;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            INextId<ARenderAble3D>.Remove(Id);
            Mesh?.Dispose();
        }

        /// <inheritdoc />
        public override void BindAndDraw(CommandBuffer commandBuffer)
        {
            Mesh?.Bind(commandBuffer);

            Mesh?.DrawIndexed(commandBuffer);
        }

        /// <inheritdoc />
        public override PipelineName PipelineName { get; } = PipelineName.PipeLine3d;
    }
}
