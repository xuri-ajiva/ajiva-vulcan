using System;
using System.Collections.Generic;
using System.Linq;
using ajiva.Helpers;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Engine;
using SharpVk;
using SharpVk.Khronos;

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
