namespace Ajiva.Components.RenderAble;

public abstract class RenderMeshIdUnique<T> : ChangingComponentBase, IRenderMesh where T : RenderMeshIdUnique<T>
{
    private uint id;
    private uint meshId;
    private bool render;

    /// <inheritdoc />
    public RenderMeshIdUnique() : base(0)
    {
        Id = INextId<T>.Next();
    }

    /// <inheritdoc />
    public virtual bool Render
    {
        get => render;
        set => ChangingObserver.RaiseAndSetIfChanged(ref render, value);
    }

    /// <inheritdoc />
    public virtual uint MeshId
    {
        get => meshId;
        set => ChangingObserver.RaiseAndSetIfChanged(ref meshId, value);
    }

    /// <inheritdoc />
    public uint Id
    {
        get => id;
        set => ChangingObserver.RaiseAndSetIfChanged(ref id, value);
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);
        INextId<T>.Remove(Id);
    }
}
