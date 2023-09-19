using System.Numerics;
using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.Physics;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform;
using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Entities;

[EntityComponent(typeof(Transform3d), typeof(TextureComponent), typeof(RenderInstanceMesh),
    typeof(CollisionsComponent), typeof(PhysicsComponent), typeof(BoundingBox))]
public partial class Cube
{
    private static readonly Random r = new Random();

    /// <inheritdoc />
    private void InitializeDefault()
    {
        Transform3d ??= new Transform3d();
        TextureComponent ??= new TextureComponent {
            TextureId = 1
        };
        RenderInstanceMesh ??= new RenderInstanceMesh(MeshPrefab.Cube, Transform3d, TextureComponent);
        PhysicsComponent ??= new PhysicsComponent {
            IsStatic = false,
            Mass = 10,
            Velocity = new Vector3(r.NextSingle(), r.NextSingle(), r.NextSingle()),
            Force = new Vector3(r.NextSingle(), -(9.8f * 9.8f), r.NextSingle()),
            Transform = Transform3d
        };
    }
}