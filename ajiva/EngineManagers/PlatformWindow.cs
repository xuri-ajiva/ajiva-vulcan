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
    public class PlatformWindow : RenderEngineComponent, IPlatformWindow
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
            Glfw3.SetWindowSizeCallback(window, (_, w, h) =>
            {
                Height = h;
                Width = w;

                OnResize.Invoke(this, EventArgs.Empty);
            });

            SharpVk.Glfw.extras.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, surfaceWidth / 2, surfaceHeight / 2, Glfw3Enum.GLFW_DONT_CARE, Glfw3Enum.GLFW_DONT_CARE);
            keyDelegate = KeyCallback;
            cursorPosDelegate = MouseCallback;
            Glfw3.SetKeyCallback(window, keyDelegate);
            Glfw3.SetCursorPosCallback(window, cursorPosDelegate);
            UpdateCursor();

            return Task.CompletedTask;
        }

        //force NO gc on these delegates by keeping an refrence
        private KeyDelegate keyDelegate;
        private CursorPosDelegate cursorPosDelegate;

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

        public void MainLoop(TimeSpan timeToRun)
        {
            var frames = 0;
            var start = DateTime.Now;

            var delta = TimeSpan.Zero;
            var now = Stopwatch.GetTimestamp();
            while (RenderEngine.Runing && !Glfw3.WindowShouldClose(window))
            {
                lock (RenderEngine.Lock)
                    OnFrame.Invoke(this, delta);

                frames++;

                if (frames % 10 == 0)
                {
                    if (DateTime.Now - start > timeToRun)
                    {
                        return;
                    }
                }

                Glfw3.PollEvents();

                if (mouseMotion)
                {
                    Glfw3.SetCursorPosition(window, Width / 2f, Height / 2f);
                    PreviousMousePosition = new(Width / 2f, Height / 2f);
                }

                var end = Stopwatch.GetTimestamp();
                delta = new(end - now);

                now = end;
            }
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
