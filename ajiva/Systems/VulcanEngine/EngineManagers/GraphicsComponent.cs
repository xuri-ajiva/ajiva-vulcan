using System.Threading;
using System.Threading.Tasks;
using ajiva.Systems.VulcanEngine.Engine;

namespace ajiva.Systems.VulcanEngine.EngineManagers
{
    public class GraphicsComponent : RenderEngineComponent
    {
        public static object CurrentGraphicsLayoutSwaoLock = new();
        public GraphicsLayout? Current { get; private set; }

        public GraphicsComponent(IRenderEngine renderEngine) : base(renderEngine)
        {
            Current = new(renderEngine);
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            EnsureGraphicsLayoutDeletion();
        }

        public void EnsureGraphicsLayoutExists()
        {
            Current ??= new(RenderEngine);
        }

        public void EnsureGraphicsLayoutDeletion()
        {
            Current?.Dispose();
            Current = null;
        }


        public void RecreateCurrentGraphicsLayoutAsync()
        {
            new Thread(() =>
            {
                var created = new GraphicsLayout(RenderEngine);
                created.EnsureExists();
                GraphicsLayout? old;
                lock (CurrentGraphicsLayoutSwaoLock)
                {
                    old = Current;
                    Current = created;
                }
                old?.Dispose();
            }).Start();
        }

        public void RecreateCurrentGraphicsLayout()
        {
            EnsureGraphicsLayoutDeletion();
            EnsureGraphicsLayoutExists();
            Current?.EnsureExists();
        }
    }
}
