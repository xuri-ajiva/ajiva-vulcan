using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Helpers;

namespace ajiva.Ecs.Example
{
    public class StdComponentSystem : ComponentSystemBase<StdComponent>, IUpdate
    {
        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            foreach (var (key, value) in ComponentEntityMap)
            {
                LogHelper.WriteLine($"[{value}]: " + key);
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
