using ajiva.Systems.VulcanEngine.Engine;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class SwapChainComponent : RenderEngineComponent
    {
        public SwapChainComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
        }
        
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            EnsureSwapChainDeletion();
        }

        public void EnsureSwapChainDeletion()
        {

        }
    }
}
