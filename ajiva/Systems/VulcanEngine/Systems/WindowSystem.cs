using System;
using System.Collections.Generic;
using System.Threading;
using ajiva.Ecs;
using ajiva.Ecs.System;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Khronos;
using Glfw3 = SharpVk.Glfw.Glfw3;
using Key = SharpVk.Glfw.Key;

namespace ajiva.Systems.VulcanEngine.Systems
{
    public class WindowSystem : SystemBase, IUpdate, IInit
    {
        public event KeyEventHandler? OnKeyEvent;
        public event EventHandler? OnResize;
        public event EventHandler<vec2>? OnMouseMove;
        public Surface? Surface { get; private set; }

        public vec2 PreviousMousePosition { get; private set; }

        public Thread WindowThread { get; }

        public Queue<Action?> WindowThreadQueue { get; } = new();
        public bool WindowReady { get; private set; }

        public WindowSystem()
        {
            keyDelegate = KeyCallback;
            cursorPosDelegate = MouseCallback;
            sizeDelegate = SizeCallback;

            PreviousMousePosition = vec2.Zero;
            mouseMotion = false;
            WindowThread = new(WindowStartup);
            WindowThread.SetApartmentState(ApartmentState.STA);
        }

        private void WindowStartup()
        {
            Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
            window = Glfw3.CreateWindow(Width, Height, "First test", MonitorHandle.Zero, WindowHandle.Zero);

            SharpVk.Glfw.extras.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, Width / 2, Height / 2, Glfw3Enum.GLFW_DONT_CARE, Glfw3Enum.GLFW_DONT_CARE);
            Glfw3.SetKeyCallback(window, keyDelegate);
            Glfw3.SetCursorPosCallback(window, cursorPosDelegate);
            Glfw3.SetWindowSizeCallback(window, sizeDelegate);
            UpdateCursor();

            WindowReady = true;
            while (!Glfw3.WindowShouldClose(window))
            {
                Thread.Sleep(1);
                while (WindowThreadQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                Glfw3.PollEvents();

                if (WindowReady) continue;
                Glfw3.DestroyWindow(window);
                return;
            }

            WindowReady = false;
        }

        private WindowHandle window;

        public void EnsureSurfaceExists()
        {
            Surface ??= Ecs.GetInstance<Instance>().CreateGlfw3Surface(window);
        }

        public void InitWindow()
        {
            Width = Ecs.GetPara<int>("SurfaceWidth");
            Height = Ecs.GetPara<int>("SurfaceHeight");
            WindowThread.Start();

            while (!WindowReady)
            {
                Thread.Sleep(1);
            }
        }

        private void SizeCallback(WindowHandle windowHandle, int width, int height)
        {
            Height = height;
            Width = width;

            OnResize?.Invoke(this, EventArgs.Empty);
        }

        //force NO gc on these delegates by keeping an reference
        private KeyDelegate keyDelegate;
        private CursorPosDelegate cursorPosDelegate;
        private WindowSizeDelegate sizeDelegate;

        public int Width { get; set; }
        public int Height { get; set; }

        public Extent2D SurfaceExtent => new((uint)Width, (uint)Height);

        private void MouseCallback(WindowHandle windowHandle, double xPosition, double yPosition)
        {
            if (!mouseMotion) return;

            var mousePos = new vec2((float)xPosition, (float)yPosition);

            if (mousePos == PreviousMousePosition)
                return;

            OnMouseMove?.Invoke(this, -(PreviousMousePosition - mousePos));
            PreviousMousePosition = mousePos;
        }

        private bool mouseMotion;

        private void KeyCallback(WindowHandle windowHandle, Key key, int scancode, InputAction inputAction, Modifier modifiers)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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

            OnKeyEvent?.Invoke(this, key, scancode, inputAction, modifiers);
        }

        private void UpdateCursor()
        {
            Glfw3.SetInputMode(window, Glfw3Enum.GLFW_CURSOR, mouseMotion ? Glfw3Enum.GLFW_CURSOR_DISABLED : Glfw3Enum.GLFW_CURSOR_NORMAL);
        }

        public void CloseWindow()
        {
            WindowReady = false;
        }

        protected override void ReleaseUnmanagedResources()
        {
            Surface?.Dispose();
            CloseWindow();
        }

        /// <inheritdoc />
        protected override void Setup()
        {
            Ecs.RegisterUpdate(this);
            Ecs.RegisterInit(this, InitPhase.Start);
        }

        public void PollEvents()
        {
            Glfw3.PollEvents();
        }

        /// <inheritdoc />
        public void Update(TimeSpan delta)
        {
            PollEvents();
            if (!WindowReady)
                Ecs.IssueClose();
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs, InitPhase phase)
        {
            InitWindow();
            EnsureSurfaceExists();
        }
    }

    public delegate void KeyEventHandler(object? sender, Key key, int scancode, InputAction inputAction, Modifier modifiers);

    public delegate void PlatformEventHandler(TimeSpan delta);
}
