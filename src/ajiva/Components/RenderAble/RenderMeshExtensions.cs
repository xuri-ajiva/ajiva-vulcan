namespace ajiva.Components.RenderAble
{
    public static class RenderMeshExtensions
    {
        public static void SetMesh(this IRenderMesh renderMesh, IMesh mesh)
        {
            renderMesh.MeshId = mesh.MeshId;
        }
    }
}