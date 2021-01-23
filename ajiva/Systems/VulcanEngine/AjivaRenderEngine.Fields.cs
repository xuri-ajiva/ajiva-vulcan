using System;
using ajiva.Systems.VulcanEngine.EngineManagers;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine
{
    public partial class AjivaRenderEngine
    {
        public AjivaRenderEngine(Instance instance)
        {
            Instance = instance;
            DeviceComponent = new(this);
            SwapChainComponent = new(this);
            ImageComponent = new(this);
            Window = new(this);
            GraphicsComponent = new(this);
            ShaderComponent = new(this);
            SemaphoreComponent = new(this);
            TextureComponent = new(this);
            Ecs = null!;
        }

        /// <inheritdoc />
        public Instance? Instance { get; set; }

        /// <inheritdoc />
        public DeviceComponent DeviceComponent { get; private set; }

        /// <inheritdoc />
        public SwapChainComponent SwapChainComponent { get; private set; }

        /// <inheritdoc />
        public PlatformWindow Window { get; private set; }

        /// <inheritdoc />
        public ImageComponent ImageComponent { get; private set; }

        /// <inheritdoc />
        public GraphicsComponent GraphicsComponent { get; private set; }

        /// <inheritdoc />
        public ShaderComponent ShaderComponent { get; private set; }

        /// <inheritdoc />
        public SemaphoreComponent SemaphoreComponent { get; private set; }

        /// <inheritdoc />
        public TextureComponent TextureComponent { get; private set; }

        /// <inheritdoc />
        public event PlatformEventHandler? OnFrame;

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
    }
}
