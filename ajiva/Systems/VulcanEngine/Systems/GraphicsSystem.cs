using System;
using System.Diagnostics;
using System.Threading;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.VulcanEngine.EngineManagers;
using ajiva.Systems.VulcanEngine.Ui;
using ajiva.Utils;
using ajiva.Utils.Changing;
using Ajiva.Wrapper.Logger;

namespace ajiva.Systems.VulcanEngine.Systems
{
    [Dependent(typeof(TextureSystem), typeof(Ajiva3dSystem), typeof(UiRenderer))]
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        public IChangingObserver ChangingObserver { get; } = new ChangingObserver(100);

        private static readonly object CurrentGraphicsLayoutSwapLock = new();
        private GraphicsLayout? Current { get; set; }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            Current?.Dispose();
            Current = null;
        }

        public void EnsureGraphicsLayoutExists()
        {
            Current ??= new(Ecs);
        }

        public void EnsureGraphicsLayoutDeletion()
        {
            Current?.DisposeIn(1000);
            Current = null;
        }

        public void RecreateCurrentGraphicsLayoutAsync()
        {
            new Thread(() =>
            {
                var created = new GraphicsLayout(Ecs);
                created.EnsureExists();
                GraphicsLayout? old;
                lock (CurrentGraphicsLayoutSwapLock)
                {
                    old = Current;
                    Current = created;
                }
                Thread.Sleep(1000);
                old?.Dispose();
            }).Start();
        }

        public void RecreateCurrentGraphicsLayout()
        {
            Ecs.GetSystem<DeviceSystem>().WaitIdle();
            var created = new GraphicsLayout(Ecs);
            created.EnsureExists();
            GraphicsLayout? old;
            lock (CurrentGraphicsLayoutSwapLock)
            {
                old = Current;
                Current = created;
            }
            ChangingObserver.Updated();
            old?.DisposeIn(1000);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            RecreateCurrentGraphicsLayout();
            Ecs.GetSystem<WindowSystem>().OnResize += RecreateCurrentGraphicsLayout;
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (ChangingObserver.UpdateCycle(delta.Iteration)) 
                RecreateCurrentGraphicsLayout();
            Current?.DrawFrame();
        }

        /// <inheritdoc />
        public GraphicsSystem(AjivaEcs ecs) : base(ecs)
        {
        }
    }
}
