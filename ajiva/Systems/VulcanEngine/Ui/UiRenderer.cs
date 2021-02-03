using System;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Systems.VulcanEngine.Engine;

namespace ajiva.Systems.VulcanEngine.Ui
{
    public class UiRenderer : ComponentSystemBase<ARenderAble2D>
    {
        /// <inheritdoc />
        protected override void Setup()
        {
        }

        /// <inheritdoc />
        public override void AttachNewComponent(IEntity entity)
        {
            entity.AddComponent(CreateComponent(entity));
        }

        /// <inheritdoc />
        public override ARenderAble2D CreateComponent(IEntity entity)
        {
            var comp = new ARenderAble2D();
            ComponentEntityMap.Add(comp, entity);
            return comp;
        }
    }
}
