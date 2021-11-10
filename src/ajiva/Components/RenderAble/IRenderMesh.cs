namespace ajiva.Components.RenderAble;

public interface IRenderMesh
{
    bool Render { get; set; }
    uint MeshId { get; set; }
    uint Id { get; set; }
}