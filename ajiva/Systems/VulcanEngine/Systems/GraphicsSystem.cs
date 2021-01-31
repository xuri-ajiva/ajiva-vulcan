using System;
using System.Threading;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Systems.VulcanEngine.EngineManagers;

namespace ajiva.Systems.VulcanEngine.Systems
{
    public class GraphicsSystem : SystemBase, IInit, IUpdate
    {
        public static readonly object CurrentGraphicsLayoutSwapLock = new();
        public GraphicsLayout? Current { get; private set; }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            EnsureGraphicsLayoutDeletion();
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
            var created = new GraphicsLayout(Ecs);
            created.EnsureExists();
            GraphicsLayout? old;
            lock (CurrentGraphicsLayoutSwapLock)
            {
                old = Current;
                Current = created;
            }

            old?.DisposeIn(1000);
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterInit(this, InitPhase.PostMain);
            Ecs.RegisterUpdate(this);
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs, InitPhase phase)
        {
            RecreateCurrentGraphicsLayout();
        }

        /// <inheritdoc />
        public void Update(TimeSpan delta)
        {
            //Console.WriteLine("Frame with " + Current?.GetHashCode());
            Current?.DrawFrame();
        }
    }
}
