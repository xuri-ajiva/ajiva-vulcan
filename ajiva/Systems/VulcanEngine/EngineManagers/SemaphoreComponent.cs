using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class SemaphoreComponent : RenderEngineComponent
    {
        public Semaphore? ImageAvailable { get; private set; }
        public Semaphore? RenderFinished { get; private set; }

        public Fence? RenderFence { get; private set; }

        public SemaphoreComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
        }

        public void EnsureSemaphoresExists()
        {
            RenderEngine.DeviceComponent.EnsureDevicesExist();
            ImageAvailable ??= RenderEngine.DeviceComponent.Device!.CreateSemaphore();
            RenderFinished ??= RenderEngine.DeviceComponent.Device!.CreateSemaphore();
            RenderFence ??= RenderEngine.DeviceComponent.Device!.CreateFence();
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            ImageAvailable?.Dispose();
            RenderFinished?.Dispose();
            RenderFence?.Dispose();
            ImageAvailable = null!;
            RenderFinished = null!;
            RenderFence = null!;
        }
    }
}
