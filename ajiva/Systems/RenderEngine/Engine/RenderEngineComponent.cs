using ajiva.Helpers;

namespace ajiva.Systems.RenderEngine.Engine
{
    public abstract class RenderEngineComponent : DisposingLogger
    {
        protected readonly IRenderEngine RenderEngine;

        public RenderEngineComponent(IRenderEngine renderEngine)
        {
            RenderEngine = renderEngine;
        }
    }
}
