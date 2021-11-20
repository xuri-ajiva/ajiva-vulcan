using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Factories;

public class DebugBoxFactory : EntityFactoryBase<DebugBox>
{
    /// <inheritdoc />
    public override DebugBox Create(IAjivaEcs system, uint id)
    {
        var box = new DebugBox { Id = id };
        //return cube.Create3DRenderedObject(system);
        system.TryAttachComponentToEntity(box, new Transform3d());
        var debugComponent = new DebugComponent
        {
            DrawTransform = true,
            DrawWireframe = true,
            Render = true
        };
        debugComponent.SetMesh(MeshPrefab.Cube);
        if (system.TryAttachComponentToEntity(box,  debugComponent))
        {

        }
        //system.AttachComponentToEntity<ATexture>(cube);
        return box;
    }

    /// <param name="disposing"></param>
    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
