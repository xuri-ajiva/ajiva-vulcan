using ajiva.Engine;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva
{
    public partial class Program : IEngine
    {
        public Program()
        {
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

        public Instance? Instance { get; set; }
        public DeviceManager DeviceManager { get; }
        public SwapChainManager SwapChainManager { get; }
        public IPlatformWindow Window { get; }
        public ImageManager ImageManager { get; }
        public GraphicsManager GraphicsManager { get; }
        public ShaderManager ShaderManager { get; }
        public BufferManager BufferManager { get; }
        public SemaphoreManager SemaphoreManager { get; }
        public TextureManager TextureManager { get; }
    }
}
