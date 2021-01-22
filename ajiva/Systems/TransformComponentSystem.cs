using System;
using System.Threading.Tasks;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;

namespace ajiva.Systems
{
    public class TransformComponentSystem : ComponentSystemBase<Transform3d>
    {
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }

        /// <inheritdoc />
        public override void Update(TimeSpan delta)
        {
        }

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
            entity.Components.Add(typeof(Transform3d), CreateComponent(entity));
        }

        /// <inheritdoc />
        public override Task Init(AjivaEcs ecs)
        {
            //todo ?=
            return Task.CompletedTask;
        }
    }
}