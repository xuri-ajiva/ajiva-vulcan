﻿namespace Ajiva.Ecs.Example;

public class StdComponentSystem : ComponentSystemBase<StdComponent>, IUpdate
{
    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        foreach (var (key, value) in ComponentEntityMap) Console.WriteLine($"[{value}]: " + key);
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromSeconds(1));

    public override StdComponent CreateComponent(IEntity entity)
    {
        return new StdComponent();
    }
}