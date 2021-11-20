namespace ajiva.Ecs.Example;

public class SomeEntityFactory : EntityFactoryBase<SdtEntity>
{
    /// <inheritdoc />
    public override SdtEntity Create(IAjivaEcs system, uint id)
    {
        var ent = new SdtEntity { Id = id };
        system.TryAttachComponentToEntity(ent, new StdComponent());
        return ent;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
