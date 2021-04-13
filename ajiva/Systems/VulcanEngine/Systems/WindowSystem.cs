using System;
using System.Collections.Generic;
using System.Threading;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Models;
using ajiva.Systems.VulcanEngine.Unions;
using ajiva.Utils;
using Ajiva.Wrapper.Logger;
using GlmSharp;
using SharpVk;
using SharpVk.Glfw;
using Glfw3 = SharpVk.Glfw.Glfw3;
using Key = SharpVk.Glfw.Key;

namespace ajiva.Systems.VulcanEngine.Systems
{
    public class WindowSystem : SystemBase, IUpdate, IInit
    {
        public event KeyEventHandler? OnKeyEvent;
        public event Action? OnResize;
        public event EventHandler<AjivaMouseMotionCallbackEventArgs>? OnMouseMove;

        private readonly Thread windowThread;
        private readonly Queue<Action?> windowThreadQueue = new();

        private bool windowReady;
        private WindowHandle window;
        private vec2 previousMousePosition = vec2.Zero;
        private AjivaEngineLayer activeLayer;

        public WindowSystem(AjivaEcs ecs) : base(ecs)
        {
            keyDelegate = KeyCallback;
            cursorPosDelegate = MouseCallback;
            sizeDelegate = SizeCallback;

            activeLayer = AjivaEngineLayer.Layer2d;
            windowThread = new(WindowStartup);
            windowThread.SetApartmentState(ApartmentState.STA);
            Canvas = new(new());
        }

        private void WindowStartup()
        {
            Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
            window = Glfw3.CreateWindow(Canvas.WidthI, Canvas.HeightI, "First test", MonitorHandle.Zero, WindowHandle.Zero);

            SharpVk.Glfw.extras.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, Canvas.WidthI / 2, Canvas.HeightI / 2, Glfw3Enum.GLFW_DONT_CARE, Glfw3Enum.GLFW_DONT_CARE);
            Glfw3.SetKeyCallback(window, keyDelegate);
            Glfw3.SetCursorPosCallback(window, cursorPosDelegate);
            Glfw3.SetWindowSizeCallback(window, sizeDelegate);
            SharpVk.Glfw.extras.Glfw3.Public.SetWindowPos_0(window.RawHandle, 2800, 500);

            UpdateCursor();

            windowReady = true;
            while (!Glfw3.WindowShouldClose(window))
            {
                Thread.Sleep(1);

                if (lastResize != DateTime.MinValue)
                {
                    if (lastResize.AddSeconds(5) > DateTime.Now)
                    {
                        OnResize?.Invoke();
                        lastResize = DateTime.MinValue;
                    }
                }

                while (windowThreadQueue.TryDequeue(out var action))
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        LogHelper.WriteLine(e);
                    }
                }
                Glfw3.PollEvents();

                if (windowReady) continue;
                Glfw3.DestroyWindow(window);
                return;
            }

            windowReady = false;
        }

        public void EnsureSurfaceExists()
        {
            if (!Canvas.HasSurface)
                Canvas.SurfaceHandle.Surface = Ecs.GetInstance<Instance>().CreateGlfw3Surface(window);
        }

        public void InitWindow()
        {
            Canvas.Width = (uint)Ecs.GetPara<int>("SurfaceWidth");
            Canvas.Height = (uint)Ecs.GetPara<int>("SurfaceHeight");
            windowThread.Start();

            while (!windowReady)
            {
                Thread.Sleep(1);
            }
        }

        private DateTime lastResize = DateTime.MinValue;

        private void SizeCallback(WindowHandle windowHandle, int width, int height)
        {
            Canvas.Height = (uint)height;
            Canvas.Width = (uint)width;

            lastResize = DateTime.Now;
        }

        //force NO gc on these delegates by keeping an reference
        private readonly KeyDelegate keyDelegate;
        private readonly CursorPosDelegate cursorPosDelegate;
        private readonly WindowSizeDelegate sizeDelegate;

        public Canvas Canvas { get; private set; }

        private void MouseCallback(WindowHandle windowHandle, double xPosition, double yPosition)
        {
            var mousePos = new vec2((float)xPosition, (float)yPosition);

            if (mousePos == previousMousePosition)
                return;

            OnMouseMove?.Invoke(this, new(mousePos, -(previousMousePosition - mousePos), activeLayer));

            previousMousePosition = mousePos;
        }

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
                    activeLayer = activeLayer switch
                    {
                        AjivaEngineLayer.Layer3d => AjivaEngineLayer.Layer2d,
                        AjivaEngineLayer.Layer2d => AjivaEngineLayer.Layer3d,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    UpdateCursor();
                    //LogHelper.WriteLine($"activeLayer: {activeLayer}");
                    break;
            }

            OnKeyEvent?.Invoke(this, key, scancode, inputAction, modifiers);
        }

        private void UpdateCursor()
        {
            Glfw3.SetInputMode(window, Glfw3Enum.GLFW_CURSOR, activeLayer == AjivaEngineLayer.Layer3d ? Glfw3Enum.GLFW_CURSOR_DISABLED : Glfw3Enum.GLFW_CURSOR_NORMAL);
        }

        public void CloseWindow()
        {
            windowReady = false;
        }

        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            Canvas.Dispose();
            CloseWindow();
        }

        public void PollEvents()
        {
            Glfw3.PollEvents();
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            PollEvents();
            if (!windowReady)
                Ecs.IssueClose();
        }

        /// <inheritdoc />
        public void Init(AjivaEcs ecs)
        {
            InitWindow();
            EnsureSurfaceExists();
        }
    }

    public delegate void KeyEventHandler(object? sender, Key key, int scancode, InputAction inputAction, Modifier modifiers);

    public record AjivaMouseMotionCallbackEventArgs(vec2 Pos, vec2 Delta, AjivaEngineLayer ActiveLayer);
}
