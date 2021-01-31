using System;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Systems;
using GlmSharp;

namespace ajiva.Systems.VulcanEngine
{
    public partial class AjivaRenderEngine : IInit
    {
        /// <inheritdoc />
        public event KeyEventHandler? OnKeyEvent;

        /// <inheritdoc />
        public event EventHandler? OnResize;

        /// <inheritdoc />
        public event EventHandler<vec2>? OnMouseMove;

        /// <inheritdoc />
        public object RenderLock { get; } = new();

        /// <inheritdoc />
        public object UpdateLock { get; } = new();

        public int DirtyComponents = 0;
        private const int MaxDirt = 10;

        private System.Collections.Generic.Queue<Action?> renderThreadQueue = new();
    }
}
