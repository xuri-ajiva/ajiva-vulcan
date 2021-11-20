using ajiva.Components.Media;
using ajiva.Components.Physics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Models;

namespace ajiva.Factories;

public class CubeFactory : EntityFactoryBase<Cube>
{
    private readonly Mesh<Vertex3D> mesh;
    private readonly MeshPool meshPool;

    public CubeFactory(MeshPool meshPool)
    {
        this.meshPool = meshPool;
        mesh = MeshPrefab.Cube;
    }

    /// <inheritdoc />
    public override Cube Create(IAjivaEcs system, uint id)
    {
        var cube = new Cube { Id = id };
        //return cube.Create3DRenderedObject(system);
        system.TryAttachComponentToEntity(cube, new Transform3d());
        var renderMesh = new RenderMesh3D();
        renderMesh.Render = true;
        renderMesh.SetMesh(mesh);

        if (system.TryAttachComponentToEntity(cube, renderMesh))
        {
        }
        if (system.TryAttachComponentToEntity(cube, new TextureComponent
            {
                TextureId = 1,
            }))
        {
        }
        if (system.TryAttachComponentToEntity(cube, new CollisionsComponent
            {
                Pool = meshPool,
                MeshId = mesh.MeshId
            }))
        {
            if (system.TryAttachComponentToEntity(cube, new BoundingBox()))
            {
            }
        }

        //system.AttachComponentToEntity<ATexture>(cube);
        return cube;
    }

    /// <param name="disposing"></param>
    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
