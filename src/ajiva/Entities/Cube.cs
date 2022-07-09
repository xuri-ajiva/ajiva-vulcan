using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using GlmSharp;

namespace ajiva.Entities;

public class Cube : DefaultEntity
{
    private static Random r = new Random();
    /// <inheritdoc />
    public Cube(IAjivaEcs ecs)
    {
        var mesh = MeshPrefab.Cube;
        var transform = this.AddComponent(new Transform3d());
        var textureComponent = this.AddComponent(new TextureComponent { TextureId = 1, });
        this.AddComponent(new RenderInstanceMesh(mesh, transform, textureComponent));
        AddComponent<CollisionsComponent, ICollider>(new CollisionsComponent { Pool = ecs.Get<MeshPool>(), MeshId = mesh.MeshId });
        var physicsComponent = AddComponent<PhysicsComponent, PhysicsComponent>(new PhysicsComponent {
            IsStatic = false,
            Mass = 10,
            Velocity = new vec3(r.NextSingle(), r.NextSingle(), r.NextSingle()),
            Force = new vec3(r.NextSingle(),-(9.8f*9.8f) , r.NextSingle()),
            Transform = transform,
        });
        AddComponent<BoundingBox, IBoundingBox>(new BoundingBox(ecs, this));
    }
}
