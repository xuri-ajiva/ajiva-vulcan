using ajiva.Utils;

namespace ajiva.Components.RenderAble
{
    public class RenderMesh2D : ChangingComponentBase, IRenderMesh
    {
        /// <inheritdoc />
        public bool Render { get; set; }

        /// <inheritdoc />
        public uint MeshId { get; set; }

        /// <inheritdoc />
        public uint Id { get; set; }

        /// <inheritdoc />
        public RenderMesh2D() : base(0)
        {
            Id = INextId<RenderMesh2D>.Next();
        }

        public void SetMesh(IMesh mesh)
        {
            MeshId = mesh.MeshId;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            base.ReleaseUnmanagedResources(disposing);
            INextId<RenderMesh2D>.Remove(Id);
        }
    }
}