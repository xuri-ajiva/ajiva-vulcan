using ajiva.Ecs;

namespace ajiva.Components.Mesh;

public interface IMeshPool : IAjivaEcsObject
{
    RenderInstanceReadyMeshPool Use();
    IMesh GetMesh(uint meshId);
    void AddMesh(IMesh mesh);
}
