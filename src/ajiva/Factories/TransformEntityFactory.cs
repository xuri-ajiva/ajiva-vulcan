using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Entities;
using GlmSharp;

namespace ajiva.Factories;

public class TransformEntityFactory : EntityFactoryBase<TransformFormEntity<Transform3d, vec3, mat4>>
{
    /// <inheritdoc />
    public override TransformFormEntity<Transform3d, vec3, mat4> Create(IAjivaEcs system, uint id)
    {
        var entity = new TransformFormEntity<Transform3d, vec3, mat4> { Id = id };
        system.TryAttachComponentToEntity(entity, new Transform3d());
        return entity;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
