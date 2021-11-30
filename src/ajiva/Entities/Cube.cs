using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models;

namespace ajiva.Entities;

public class Cube : DefaultEntity
{
    private readonly MeshPool meshPool;
    private readonly Mesh<Vertex3D> mesh;

    /// <inheritdoc />
    public Cube(IAjivaEcs ecs)
    {
        if (meshPool == null)
        {
            meshPool = ecs.Get<MeshPool>();
            mesh = MeshPrefab.Cube;
        }

        this.AddComponent(new Transform3d());
        this.AddComponent(new RenderInstanceMesh(instanceMeshPool.CreateInstance(instancedMesh)));
        this.AddComponent(new TextureComponent { TextureId = 1, });
        AddComponent<CollisionsComponent, ICollider>(new CollisionsComponent { Pool = meshPool, MeshId = mesh.MeshId });
        AddComponent<BoundingBox, IBoundingBox>(new BoundingBox(ecs, this));
    }
}
