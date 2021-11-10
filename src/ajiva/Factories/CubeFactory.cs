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
    private readonly MeshPool meshPool;
    private readonly Mesh<Vertex3D> mesh;

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
        system.TryAttachNewComponentToEntity<Transform3d>(cube, out _);
        if (system.TryAttachNewComponentToEntity<RenderMesh3D>(cube, out var renderMesh))
        {
            renderMesh.Render = true;
            renderMesh.SetMesh(mesh);
        }
        if (system.TryAttachNewComponentToEntity<TextureComponent>(cube, out var textureComponent))
        {
            textureComponent.TextureId = 1;
        }
        if (system.TryAttachNewComponentToEntity<CollisionsComponent>(cube, out var colider))
        {
            if (system.TryAttachNewComponentToEntity<BoundingBox>(cube, out var boundingBox))
            {
            }
            colider.Pool = meshPool;
            colider.MeshId = mesh.MeshId;
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