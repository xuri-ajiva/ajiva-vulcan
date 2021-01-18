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
        public uint Id { get; }
        private IRenderEngine? render;
        public Mesh? Mesh { get; private set; }
        public bool Render { get; set; }
        public const int DoNotRenderId = -1;

        private static int currentMaxId = 0;

        public static int NextId() => currentMaxId++;

        public ARenderAble(Mesh? mesh, int id)
        {
            Mesh = mesh;
            if (id >= 0)
            {
                Id = (uint)id;
                Render = true;
                ATrace.LockInline($"Creating ARenderAble with id {Id}");
            }
            else
            {
                Render = false;
                ATrace.LockInline("Creating ARenderAble but nor Rendering");
            }
        }

        public void BindAndDraw(CommandBuffer commandBuffer)
        {
            ATrace.Assert(Mesh != null, nameof(Mesh) + " != null");
            ATrace.Assert(render != null, nameof(render) + " != null");

            Mesh.Bind(commandBuffer);

            commandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, render.GraphicsComponent.Current!.PipelineLayout, 0, render.GraphicsComponent.Current.DescriptorSet, Id * (uint)Unsafe.SizeOf<UniformModel>());

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
            currentMaxId--;
            Mesh?.Dispose();
        }
    }
}
