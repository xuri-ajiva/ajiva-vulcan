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
        }
        public void CreateSemaphores()
        {
            ImageAvailable = engine.DeviceManager.Device.CreateSemaphore();
            RenderFinished = engine.DeviceManager.Device.CreateSemaphore();
        }
    }
}
