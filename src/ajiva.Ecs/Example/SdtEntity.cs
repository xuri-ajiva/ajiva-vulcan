using System;
using System.Threading;

namespace ajiva.Ecs.Example;

public class SdtEntity : AEntity, IUpdate
{
    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        if (this.TryGetComponent<StdComponent>(out var health))
        {
            health.Health += new Random().Next(-10, 10);
        }
    }

    /// <inheritdoc />
    public PeriodicTimer Timer { get; } = new PeriodicTimer(TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
    }
}
