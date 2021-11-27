using System;

namespace ajiva.Ecs.Example;

public class StdComponentSystem : ComponentSystemBase<StdComponent>, IUpdate
{
    /// <inheritdoc />
    /// <inheritdoc />
    public StdComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        foreach (var (key, value) in ComponentEntityMap)
        {
            Console.WriteLine($"[{value}]: " + key);
        }
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromSeconds(1));
}
