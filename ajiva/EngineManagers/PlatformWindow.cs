using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ajiva.Engine;
using GlmSharp;
using SharpVk.Glfw;
using SharpVk.Khronos;
using Glfw3 = SharpVk.Glfw.Glfw3;
using Key = SharpVk.Glfw.Key;

namespace ajiva.EngineManagers
{
    public class PlatformWindow : RenderEngineComponent
    {
        public event PlatformEventHandler OnFrame;
        public event KeyEventHandler OnKeyEvent;
        public event EventHandler OnResize;
        public event EventHandler<vec2> OnMouseMove;
        public Surface Surface { get; private set; }

        public vec2 PreviousMousePosition { get; private set; }

        public PlatformWindow(IRenderEngine renderEngine) : base(renderEngine)
        {
            OnFrame = null!;
            OnKeyEvent = null!;
            OnResize = null!;
            OnMouseMove = null!;
            Surface = null!;
            PreviousMousePosition = vec2.Zero;
            mouseMotion = false;
        }

        private WindowHandle window;

        public void CreateSurface()
        {
            Surface = RenderEngine.Instance.CreateGlfw3Surface(window);
        }

        public Task InitWindow(int surfaceWidth, int surfaceHeight)
        {
            Width = surfaceWidth;
            Height = surfaceHeight;

            Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
            window = Glfw3.CreateWindow(surfaceWidth, surfaceHeight, "First test", MonitorHandle.Zero, WindowHandle.Zero);

            SharpVk.Glfw.extras.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, surfaceWidth / 2, surfaceHeight / 2, Glfw3Enum.GLFW_DONT_CARE, Glfw3Enum.GLFW_DONT_CARE);
            keyDelegate = KeyCallback;
            cursorPosDelegate = MouseCallback;
            sizeDelegate = SizeCallback;
            Glfw3.SetKeyCallback(window, keyDelegate);
            Glfw3.SetCursorPosCallback(window, cursorPosDelegate);
            Glfw3.SetWindowSizeCallback(window, sizeDelegate);
            UpdateCursor();

            return Task.CompletedTask;
        }

        private void SizeCallback(WindowHandle windowHandle, int width, int height)
        {
            Height = height;
            Width = width;

            OnResize.Invoke(this, EventArgs.Empty);
        }

        //force NO gc on these delegates by keeping an reference
        private KeyDelegate keyDelegate;
        private CursorPosDelegate cursorPosDelegate;
        private WindowSizeDelegate sizeDelegate;

        public int Width { get; set; }

        public int Height { get; set; }

        public uint SurfaceWidth => (uint)Width;
        public uint SurfaceHeight => (uint)Height;

        private void MouseCallback(WindowHandle windowHandle, double xPosition, double yPosition)
        {
            if (!mouseMotion) return;

            var mousePos = new vec2((float)xPosition, (float)yPosition);

            if (mousePos == PreviousMousePosition)
                return;

            OnMouseMove.Invoke(this, -(PreviousMousePosition - mousePos));
            PreviousMousePosition = mousePos;
        }

        private bool mouseMotion;

        private void KeyCallback(WindowHandle windowHandle, Key key, int scancode, InputAction inputAction, Modifier modifiers)
        {
            switch (key)
            {
                //todo dev only
                case Key.Escape:
                    Environment.Exit(0);
                    break;
                case Key.Tab when inputAction == InputAction.Press:
                    mouseMotion = !mouseMotion;
                    UpdateCursor();
                    Console.WriteLine($"mouseMotion: {mouseMotion}");
                    break;
            }

            OnKeyEvent.Invoke(this, key, scancode, inputAction, modifiers);
        }

        private void UpdateCursor()
        {
            Glfw3.SetInputMode(window, Glfw3Enum.GLFW_CURSOR, mouseMotion ? Glfw3Enum.GLFW_CURSOR_DISABLED : Glfw3Enum.GLFW_CURSOR_NORMAL);
        }

        private async Task RunDelta(PlatformEventHandler action, Func<bool> condition, TimeSpan maxToRun)
        {
            var iteration = 0u;
            var start = DateTime.Now;

            var delta = TimeSpan.Zero;
            var now = Stopwatch.GetTimestamp();
            while (condition())
            {
                action?.Invoke(this, delta);

                iteration++;

                if (iteration % 10 == 0)
                {
                    if (DateTime.Now - start > maxToRun)
                    {
                        return;
                    }
                }

                var end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
        }

        public async Task RenderLoop(TimeSpan timeToRun)
        {
            await RunDelta(delegate(object sender, TimeSpan delta)
            {
                lock (RenderEngine.RenderLock)
                    OnFrame.Invoke(this, delta);
                Glfw3.PollEvents();
            }, () => RenderEngine.Runing && !Glfw3.WindowShouldClose(window), timeToRun);
        }

        public void CloseWindow()
        {
            Glfw3.DestroyWindow(window);
        }

        protected override void ReleaseUnmanagedResources()
        {
            Surface.Dispose();
            CloseWindow();
        }
    }

    public delegate void KeyEventHandler(object? sender, Key key, int scancode, InputAction inputAction, Modifier modifiers);

    public delegate void PlatformEventHandler(object sender, TimeSpan delta);
}
