using ajiva.Components.Physics;
using ajiva.Ecs;

namespace ajiva.Systems.Physics;

[Dependent(typeof(TransformComponentSystem))]
public class CollisionsComponentSystem  : ComponentSystemBase<CollisionsComponent>
{
    /// <inheritdoc />
    public CollisionsComponentSystem(IAjivaEcs ecs) : base(ecs)
    {
    }
}