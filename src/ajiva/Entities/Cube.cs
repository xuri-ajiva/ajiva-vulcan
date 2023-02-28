using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs.Entity.Helper;
using ajiva.Worker;
using GlmSharp;

namespace ajiva.Entities;

[EntityComponent(typeof(Transform3d), typeof(TextureComponent), typeof(RenderInstanceMesh),
    typeof(CollisionsComponent), typeof(PhysicsComponent), typeof(BoundingBox))]
public partial class Cube
{
    private static Random r = new Random();

    /// <inheritdoc />
    public Cube(IWorkerPool workerPool, MeshPool meshPool)
    {
        var mesh = MeshPrefab.Cube;

        Transform3d = new Transform3d();
        TextureComponent = new TextureComponent() {
            TextureId = 1
        };
        RenderInstanceMesh = new RenderInstanceMesh(mesh, Transform3d, TextureComponent);
        CollisionsComponent = new CollisionsComponent() {
            Pool = meshPool,
            MeshId = mesh.MeshId
        };
        PhysicsComponent = new PhysicsComponent() {
            IsStatic = false,
            Mass = 10,
            Velocity = new vec3(r.NextSingle(), r.NextSingle(), r.NextSingle()),
            Force = new vec3(r.NextSingle(), -(9.8f * 9.8f), r.NextSingle()),
            Transform = Transform3d,
        };
        BoundingBox = new BoundingBox(this, workerPool);
    }
}
