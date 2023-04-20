namespace ajiva.Components.Mesh;

public interface IMeshPool
{
    RenderInstanceReadyMeshPool Use();
    IMesh GetMesh(uint meshId);
    void AddMesh(IMesh mesh);
}
