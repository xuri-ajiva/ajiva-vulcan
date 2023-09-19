using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Ecs.Example;

[EntityComponent(typeof(StdComponent))]
public partial class SdtEntity : DisposingLogger, IUpdate
{
    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (TryGetComponent<StdComponent>(out var health)) health.Health += new Random().Next(-10, 10);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromSeconds(1));

    protected void InitializeDefault()
    {
        StdComponent ??= new StdComponent();
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}