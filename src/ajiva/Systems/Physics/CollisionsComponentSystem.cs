using ajiva.Components.Physics;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Utils;

namespace ajiva.Systems.Physics
{
    [Dependent(typeof(TransformComponentSystem))]
    public class CollisionsComponentSystem  : ComponentSystemBase<CollisionsComponent>
    {
        /// <inheritdoc />
        public CollisionsComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
}
