using System;
using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva
{
    public partial class Program : IEngine, IDisposable
    {
        public Program(Instance instance)
        {
            Instance = instance;
            DeviceManager = new(this);
            SwapChainManager = new(this);
            ImageManager = new(this);
            Window = new PlatformWindow(this);
            GraphicsManager = new(this);
            ShaderManager = new(this);
            BufferManager = new(this);
            SemaphoreManager = new(this);
            TextureManager = new(this);
        }

        //public static Instance? Instance { get; set; }
        /// <inheritdoc />
        public bool Runing { get; set; }
        /// <inheritdoc />
        public Instance? Instance { get; set; }
        /// <inheritdoc />
        public DeviceManager DeviceManager { get; }
        /// <inheritdoc />
        public SwapChainManager SwapChainManager { get; }
        /// <inheritdoc />
        public IPlatformWindow Window { get; }
        /// <inheritdoc />
        public ImageManager ImageManager { get; }
        /// <inheritdoc />
        public GraphicsManager GraphicsManager { get; }
        /// <inheritdoc />
        public ShaderManager ShaderManager { get; }
        /// <inheritdoc />
        public BufferManager BufferManager { get; }
        /// <inheritdoc />
        public SemaphoreManager SemaphoreManager { get; }
        /// <inheritdoc />
        public TextureManager TextureManager { get; }
    }
}
