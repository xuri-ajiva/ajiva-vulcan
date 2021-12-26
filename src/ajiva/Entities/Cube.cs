using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;

namespace ajiva.Entities;

public class Cube : DefaultEntity
{
    /// <inheritdoc />
    public Cube(IAjivaEcs ecs)
    {
        var mesh = MeshPrefab.Cube;
        var transform = this.AddComponent(new Transform3d());
        var textureComponent = this.AddComponent(new TextureComponent { TextureId = 1, });
        this.AddComponent(new RenderInstanceMesh(mesh, transform, textureComponent));
        AddComponent<CollisionsComponent, ICollider>(new CollisionsComponent { Pool = ecs.Get<MeshPool>(), MeshId = mesh.MeshId });
        AddComponent<BoundingBox, IBoundingBox>(new BoundingBox(ecs, this));
    }
}
