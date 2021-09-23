using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;

namespace ajiva.Systems
{
    public class TransformComponentSystem : ComponentSystemBase<Transform3d>
    {
        /// <inheritdoc />
        public override Transform3d CreateComponent(IEntity entity)
        {
            var tra = new Transform3d();
            ComponentEntityMap.Add(tra, entity);
            return tra;
        }

        public TransformComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
    public class Transform2dComponentSystem : ComponentSystemBase<Transform2d>
    {
        /// <inheritdoc />
        public override Transform2d CreateComponent(IEntity entity)
        {
            var tra = new Transform2d();
            ComponentEntityMap.Add(tra, entity);
            return tra;
        }
        

        public Transform2dComponentSystem(IAjivaEcs ecs) : base(ecs)
        {
        }
    }
}
