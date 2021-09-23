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

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        public TransformComponentSystem(AjivaEcs ecs) : base(ecs)
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

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        public Transform2dComponentSystem(AjivaEcs ecs) : base(ecs)
        {
        }
    }
}
