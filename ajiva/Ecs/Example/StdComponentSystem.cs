using System;
using System.Threading.Tasks;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;

namespace ajiva.Ecs.Example
{
    public class StdComponentSystem : ComponentSystemBase<StdComponent>
    {
        /// <inheritdoc />
        public override void Update(TimeSpan delta)
        {
            foreach (var (key, value) in ComponentEntityMap)
            {
                Console.WriteLine($"[{value}]: " + key);
            }
        }

        /// <inheritdoc />
        public override async Task Init(AjivaEcs ecs)
        {
        }

        /// <inheritdoc />
        public override StdComponent CreateComponent(IEntity entity)
        {
            var cmp = new StdComponent {Health = 100};
            ComponentEntityMap.Add(cmp, entity);
            return cmp;
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            var cmp = new StdComponent {Health = 100};
            ComponentEntityMap.Add(cmp, entity);
            entity.Components.Add(typeof(StdComponent),cmp);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }
    }
}
