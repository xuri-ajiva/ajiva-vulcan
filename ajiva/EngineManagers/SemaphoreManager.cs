using System;
using ajiva.Engine;
using SharpVk;

namespace ajiva.EngineManagers
{
    public class SemaphoreManager : IEngineManager
    {
        private readonly IEngine engine;

        public Semaphore ImageAvailable { get; private set; }
        public Semaphore RenderFinished { get; private set; }

        public SemaphoreManager(IEngine engine)
        {
            this.engine = engine;
            RenderFinished = null!;
            ImageAvailable = null!;
        }
        public void CreateSemaphores()
        {
            ImageAvailable = engine.DeviceManager.Device.CreateSemaphore();
            RenderFinished = engine.DeviceManager.Device.CreateSemaphore();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ImageAvailable.Dispose();
            RenderFinished.Dispose();
            ImageAvailable = null!;
            RenderFinished = null!;
            GC.SuppressFinalize(this);
        }
    }
}
