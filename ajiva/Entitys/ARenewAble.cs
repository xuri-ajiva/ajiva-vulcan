using System.Diagnostics;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.Entitys
{
    public class ARenewAble : DisposingLogger
    {
        private IRenderEngine? render;
        public Mesh? Mesh { get; private set; }

        public ARenewAble(Mesh? mesh)
        {
            Mesh = mesh;
        }

        public void BindAndDraw(CommandBuffer commandBuffer)
        {
            ATrace.Assert(Mesh != null, nameof(Mesh) + " != null");
            ATrace.Assert(render != null, nameof(render) + " != null");

            Mesh.Bind(commandBuffer);
            
            commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, render.GraphicsComponent.PipelineLayout, 0, render.GraphicsComponent.DescriptorSet, null);

            Mesh.DrawIndexed(commandBuffer);
        }

        public void Create(IRenderEngine renderEngine)
        {
            render = renderEngine;
            Mesh?.Create(renderEngine.DeviceComponent);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Mesh?.Dispose();
        }
    }
}
