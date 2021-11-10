using System;

namespace ajiva.Ecs.Example;

public class StdComponentSystem : ComponentSystemBase<StdComponent>, IUpdate
{
    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        foreach (var (key, value) in ComponentEntityMap)
        {
            Console.WriteLine($"[{value}]: " + key);
        }
    }

    /// <inheritdoc />
    public override StdComponent CreateComponent(IEntity entity)
    {
        var cmp = new StdComponent {Health = 100};
        return RegisterComponent(entity, cmp);
    }

    /// <inheritdoc />


    /// <inheritdoc />
    public StdComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }
}