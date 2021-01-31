using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Systems;
using GlmSharp;
using SharpVk.Glfw;

namespace ajiva.Systems.VulcanEngine
{
    public partial class AjivaRenderEngine : IInit
    {
        #region Public

        public void Cleanup()
        {
            lock (UpdateLock)
            lock (RenderLock)
            {
                Ecs.GetSystem<DeviceSystem>().WaitIdle();

                mainCamara?.Dispose();
            }
        }

        public void RecreateSwapChain()
        {
            lock (UpdateLock)
            lock (RenderLock)
            {
                Ecs.GetSystem<DeviceSystem>().WaitIdle();

                var window = Ecs.GetSystem<WindowSystem>();

                mainCamara?.UpdatePerspective(mainCamara.Fov, window.Width, window.Height);
                
                Ecs.GetSystem<GraphicsSystem>().EnsureGraphicsLayoutDeletion();
                Ecs.GetSystem<GraphicsSystem>().EnsureGraphicsLayoutExists();
                Ecs.GetSystem<GraphicsSystem>().Current!.EnsureExists();
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Cleanup();
            base.ReleaseUnmanagedResources();
        }

        #endregion
        
        public void InitWindow()
        {
            var window = Ecs.GetSystem<WindowSystem>();

            window.OnResize += (_, eventArgs) =>
            {
                RecreateSwapChain();
                OnResize?.Invoke(this, eventArgs);
            };

            window.OnKeyEvent += delegate(object? _, Key key, int scancode, InputAction action, Modifier modifier)
            {
                var down = action != InputAction.Release;

                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (key)
                {
                    case Key.W:
                        MainCamara.Keys.up = down;
                        break;
                    case Key.D:
                        MainCamara.Keys.right = down;
                        break;
                    case Key.S:
                        MainCamara.Keys.down = down;
                        break;
                    case Key.A:
                        MainCamara.Keys.left = down;
                        break;
                }

                OnKeyEvent?.Invoke(this, key, scancode, action, modifier);
            };

            window.OnMouseMove += delegate(object? _, vec2 vec2)
            {
                MainCamara.OnMouseMoved(vec2.x, vec2.y);
                OnMouseMove?.Invoke(this, vec2);
            };
        }
    }
}
