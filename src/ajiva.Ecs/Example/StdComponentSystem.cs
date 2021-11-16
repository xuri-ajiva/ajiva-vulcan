using System;
using System.Threading;

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
    public PeriodicTimer Timer { get; } = new PeriodicTimer(TimeSpan.FromSeconds(1));

    /// <inheritdoc />
    public override StdComponent CreateComponent(IEntity entity)
    {
        var cmp = new StdComponent { Health = 100 };
        return RegisterComponent(entity, cmp);
    }
}
