using System;
using ajiva.Systems.RenderEngine.EngineManagers;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.RenderEngine
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
        }

        /// <inheritdoc />
        public Instance? Instance { get; set; }

        /// <inheritdoc />
        public DeviceComponent DeviceComponent { get; }

        /// <inheritdoc />
        public SwapChainComponent SwapChainComponent { get; }

        /// <inheritdoc />
        public PlatformWindow Window { get; }

        /// <inheritdoc />
        public ImageComponent ImageComponent { get; }

        /// <inheritdoc />
        public GraphicsComponent GraphicsComponent { get; }

        /// <inheritdoc />
        public ShaderComponent ShaderComponent { get; }

        /// <inheritdoc />
        public SemaphoreComponent SemaphoreComponent { get; }

        /// <inheritdoc />
        public TextureComponent TextureComponent { get; }

        /// <inheritdoc />
        public event PlatformEventHandler OnFrame;

        /// <inheritdoc />
        public event KeyEventHandler OnKeyEvent;

        /// <inheritdoc />
        public event EventHandler OnResize;

        /// <inheritdoc />
        public event EventHandler<vec2> OnMouseMove;

        /// <inheritdoc />
        public object RenderLock { get; } = new();

        /// <inheritdoc />
        public object UpdateLock { get; } = new();
    }
}