using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ajiva.Engine;
using ajiva.Models;
using SharpVk;

namespace ajiva.Entity
{
    public class ARenderAble : DisposingLogger
    {
        private IRenderEngine? render;
        public Mesh? Mesh { get; private set; }

        public static int NextId() => currentMaxId++;

        public ARenderAble(Mesh? mesh, int id)
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
