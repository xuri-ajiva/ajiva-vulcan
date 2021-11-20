using ajiva.Ecs;

namespace ajiva.Components.RenderAble;

public interface IMeshPool : IAjivaEcsObject
{
    Dictionary<uint, IMesh> Meshes { get; }
    RenderInstanceReadyMeshPool Use();
    IMesh GetMesh(uint meshId);
    void AddMesh(IMesh mesh);
}
