using System;
using ajiva.Ecs.Entity.Helper;

namespace ajiva.Ecs.Example;

[EntityComponent(typeof(StdComponent))]
public partial class SdtEntity : DisposingLogger, IUpdate
{
    protected void InitializeDefault()
    {
        StdComponent ??= new StdComponent();
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (this.TryGetComponent<StdComponent>(out var health))
        {
            health.Health += new Random().Next(-10, 10);
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
