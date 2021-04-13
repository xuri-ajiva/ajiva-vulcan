﻿using ajiva.Components.Media;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;

namespace ajiva.Systems
{
    public class TransformComponentSystem : ComponentSystemBase<Transform3d>
    {

        /// <inheritdoc />
        public override Transform3d CreateComponent(IEntity entity)
        {
            var tra = Transform3d.Default;
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
}
