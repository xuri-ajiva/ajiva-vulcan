using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Models.Vertex;

namespace ajiva.Entities;

public class Cube : DefaultEntity
{
    private static MeshPool meshPool;
    private static Mesh<Vertex3D> mesh;
    private static InstanceMeshPool instanceMeshPool;
    private static IInstancedMesh instancedMesh;

    private static object initLock = new();
    private static bool isInit = false;

    /// <inheritdoc />
    public Cube(IAjivaEcs ecs)
    {
        lock (initLock)
        {
            if (!isInit)
            {
                meshPool = ecs.Get<MeshPool>();
                instanceMeshPool = ecs.Get<InstanceMeshPool>();
                mesh = MeshPrefab.Cube;
                instancedMesh = instanceMeshPool.AsInstanced(mesh);
                isInit = true;
            }
        }

        this.AddComponent(new Transform3d());
        this.AddComponent(new RenderInstanceMesh(instanceMeshPool.CreateInstance(instancedMesh)));
        this.AddComponent(new TextureComponent { TextureId = 1, });
        AddComponent<CollisionsComponent, ICollider>(new CollisionsComponent { Pool = meshPool, MeshId = mesh.MeshId });
        AddComponent<BoundingBox, IBoundingBox>(new BoundingBox(ecs, this));
    }
}
