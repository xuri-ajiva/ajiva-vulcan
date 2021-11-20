using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Entities;

namespace ajiva.Factories;

public class RectFactory : EntityFactoryBase<Rect>
{
    /// <inheritdoc />
    public override Rect Create(IAjivaEcs system, uint id)
    {
        var rect = new Rect { Id = id };
        system.TryAttachComponentToEntity(rect, new Transform2d());
        var renderMesh = new RenderMesh2D
        {
            Render = true
        };
        renderMesh.SetMesh(MeshPrefab.Rect);

        if (system.TryAttachComponentToEntity(rect, renderMesh))
        {
        }
        //system.AttachComponentToEntity<ATexture>(cube);
        return rect;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
