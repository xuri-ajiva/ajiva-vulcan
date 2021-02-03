using System;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;

namespace ajiva.Ecs.Example
{
    public class StdComponentSystem : ComponentSystemBase<StdComponent>, IUpdate
    {
        /// <inheritdoc />
        public void Update(TimeSpan delta)
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
            ComponentEntityMap.Add(cmp, entity);
            return cmp;
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterUpdate(this);
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            var cmp = new StdComponent {Health = 100};
            ComponentEntityMap.Add(cmp, entity);
            entity.AddComponent(cmp);
        }
    }
}
