using System.Collections.Generic;
using ajiva.Engine;
using ajiva.Entity;
using ajiva.Models;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class AEntityComponent : RenderEngineComponent
    {
        private readonly IRenderEngine renderEngine;

        public object EntityLock { get; } = new();
        public List<AEntity> Entities { get; } = new();

        public AEntityComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            this.renderEngine = renderEngine;
        }

        protected override void ReleaseUnmanagedResources()
        {
            lock (EntityLock)
            {
                foreach (var entity in Entities)
                {
                    entity.Dispose();
                }
            }
        }

        public void BindAllAndDraw(CommandBuffer commandBuffer)
        {
            lock (EntityLock)
            {
                foreach (var mesh in Entities)
                {
                    mesh.RenderAble.BindAndDraw(commandBuffer);
                }
            }
        }
    }
}
