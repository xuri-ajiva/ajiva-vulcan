using System;
using System.Threading.Tasks;
using GlmSharp;
using SharpVk.Glfw;

namespace ajiva.Systems.VulcanEngine
{
    public partial class AjivaRenderEngine
    {
        #region Public

        public void Cleanup()
        {
            lock (UpdateLock)
            lock (RenderLock)
            {
                DeviceComponent.WaitIdle();

                SwapChainComponent.Dispose();
                ImageComponent.Dispose();
                GraphicsComponent.Dispose();
                ShaderComponent.Dispose();
                SemaphoreComponent.Dispose();
                TextureComponent.Dispose();
                Window.Dispose();
                DeviceComponent.Dispose();
            }
        }

        private void CleanupSwapChain()
        {
            ImageComponent.EnsureDepthResourcesDeletion();

            SwapChainComponent.EnsureSwapChainDeletion();

            GraphicsComponent.EnsureGraphicsLayoutDeletion();
        }

        public void RecreateSwapChain()
        {
            lock (UpdateLock)
            lock (RenderLock)
            {
                DeviceComponent.WaitIdle();
                CleanupSwapChain();

                MainCamara.UpdatePerspective(mainCamara.Fov, Window.Width, Window.Height);
                ImageComponent.EnsureDepthResourcesExits();
                GraphicsComponent.EnsureGraphicsLayoutExists();
                GraphicsComponent.Current!.EnsureExists();
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Cleanup();
        }

        #endregion

        public async Task InitVulkan()
        {
            lock (RenderLock)
            {
                Window.EnsureSurfaceExists();
                DeviceComponent.EnsureDevicesExist();
                DeviceComponent.EnsureCommandPoolsExists();
                ShaderComponent.EnsureCreateUniformBufferExists();
                
                TextureComponent.EnsureDefaultImagesExists();
                ImageComponent.EnsureDepthResourcesExits();

                GraphicsComponent.EnsureGraphicsLayoutExists();
                GraphicsComponent.Current!.EnsureExists();

                SemaphoreComponent.EnsureSemaphoresExists();
                GC.Collect();
            }
        }

        public async Task InitWindow(int surfaceWidth, int surfaceHeight)
        {
            await Window.InitWindow(surfaceWidth, surfaceHeight);

            Window.OnResize += (_, eventArgs) =>
            {
                RecreateSwapChain();
                OnResize?.Invoke(this, eventArgs);
            };

            Window.OnKeyEvent += delegate(object? _, Key key, int scancode, InputAction action, Modifier modifier)
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

            Window.OnMouseMove += delegate(object? _, vec2 vec2)
            {
                MainCamara.OnMouseMoved(vec2.x, vec2.y);
                OnMouseMove?.Invoke(this, vec2);
            };
        }
    }
}
