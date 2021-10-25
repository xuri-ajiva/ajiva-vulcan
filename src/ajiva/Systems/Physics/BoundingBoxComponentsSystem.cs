using ajiva.Components.Physics;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Utils;

namespace ajiva.Systems.Physics
{
    [Dependent(typeof(CollisionsComponentSystem))]
    public class BoundingBoxComponentsSystem : ComponentSystemBase<BoundingBox>, IUpdate
    {
        /// <inheritdoc />
        public BoundingBoxComponentsSystem(IAjivaEcs ecs) : base(ecs) { }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
        }

        /// <inheritdoc />
        public override BoundingBox RegisterComponent(IEntity entity, BoundingBox component)
        {
            component.Ecs = Ecs;
            if (entity.TryGetComponent<Transform3d>(out var transform))
            {
                component.Transform = transform;
            }
            if (entity.TryGetComponent<CollisionsComponent>(out var collider))
            {
                component.Collider = collider;
            }

            return base.RegisterComponent(entity, component);
        }
    }
}
