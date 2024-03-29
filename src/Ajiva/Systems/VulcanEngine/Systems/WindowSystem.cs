﻿using System.Numerics;
using Ajiva.Ecs;
using Ajiva.Systems.VulcanEngine.Interfaces;
using Ajiva.Systems.VulcanEngine.Layer;
using SharpVk;
using SharpVk.Glfw;
using SharpVk.Glfw.extras;
using Glfw3 = SharpVk.Glfw.Glfw3;
using Key = SharpVk.Glfw.Key;

namespace Ajiva.Systems.VulcanEngine.Systems;

public class WindowSystem : SystemBase, IUpdate, IWindowSystem
{
    private readonly Instance _instance;
    private readonly ILifetimeManager _lifetimeManager;
    private readonly CursorPosDelegate cursorPosDelegate;

    //force NO gc on these delegates by keeping an reference
    private readonly KeyDelegate keyDelegate;
    private readonly WindowSizeDelegate sizeDelegate;

    private readonly WindowConfig windowConfig;

    private readonly Thread windowThread;
    private readonly Queue<Action?> windowThreadQueue = new Queue<Action?>();
    private AjivaEngineLayer activeLayer;

    private DateTime lastResize = DateTime.MinValue;
    private Vector2 previousMousePosition = Vector2.Zero;
    private Extent2D priviesSize;
    private WindowHandle window;

    private bool windowReady;

    public WindowSystem(AjivaConfig config, Instance instance, ILifetimeManager lifetimeManager)
    {
        _instance = instance;
        _lifetimeManager = lifetimeManager;
        keyDelegate = KeyCallback;
        cursorPosDelegate = MouseCallback;
        sizeDelegate = SizeCallback;

        activeLayer = AjivaEngineLayer.Layer2d;
        windowThread = new Thread(WindowStartup);
        windowThread.SetApartmentState(ApartmentState.STA);
        Canvas = new Canvas(new SurfaceHandle());

        windowConfig = config.WindowConfig;

        OnResize += (_, size, newSize) => Log.Information("Resized from [w: {Width}, h: {Height}] to [w: {Width}, h: {Height}]", size.Width, size.Height, newSize.Width, newSize.Height);

        InitWindow();
        EnsureSurfaceExists();
    }

    /// <inheritdoc />
    public void Update(UpdateInfo delta)
    {
        PollEvents();
        if (!windowReady)
            _lifetimeManager.IssueClose();
    }

    /// <inheritdoc />
    public PeriodicUpdateInfo Info { get; } = new PeriodicUpdateInfo(TimeSpan.FromMilliseconds(5));

    public Canvas Canvas { get; }

    public event KeyEventHandler? OnKeyEvent;
    public event WindowResizedDelegate OnResize;
    public event EventHandler<AjivaMouseMotionCallbackEventArgs>? OnMouseMove;

    public void EnsureSurfaceExists()
    {
        if (!Canvas.HasSurface)
            Canvas.SurfaceHandle.Surface = _instance.CreateGlfw3Surface(window);
    }

    public void InitWindow()
    {
        Canvas.Height = windowConfig.Height;
        Canvas.Width = windowConfig.Width;
        windowThread.Start();

        while (!windowReady) Thread.Sleep(1);
    }

    public void CloseWindow()
    {
        windowReady = false;
    }

    public void PollEvents()
    {
        Glfw3.PollEvents();
    }

    private void WindowStartup()
    {
        Glfw3.WindowHint(WindowAttribute.ClientApi, 0);
        window = Glfw3.CreateWindow(Canvas.WidthI, Canvas.HeightI, "First test", MonitorHandle.Zero, WindowHandle.Zero);
        SharpVk.Glfw.extras.Glfw3.Public.SetWindowSizeLimits_0(window.RawHandle, Canvas.WidthI / 2, Canvas.HeightI / 2, Glfw3Enum.GLFW_DONT_CARE, Glfw3Enum.GLFW_DONT_CARE);
        Glfw3.SetKeyCallback(window, keyDelegate);
        Glfw3.SetCursorPosCallback(window, cursorPosDelegate);
        Glfw3.SetWindowSizeCallback(window, sizeDelegate);
        SharpVk.Glfw.extras.Glfw3.Public.SetWindowPos_0(window.RawHandle, windowConfig.PosX, windowConfig.PosY);

        UpdateCursor();

        windowReady = true;
        while (!Glfw3.WindowShouldClose(window))
        {
            Thread.Sleep(1);

            if (lastResize != DateTime.MinValue)
                if (lastResize.AddSeconds(5) > DateTime.Now)
                {
                    OnResize?.Invoke(this, priviesSize, Canvas.Extent);
                    priviesSize = Canvas.Extent;
                    lastResize = DateTime.MinValue;
                }

            while (windowThreadQueue.TryDequeue(out var action))
                try
                {
                    action?.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                }
            SharpVk.Glfw.extras.Glfw3.WaitEventsTimeout(1);

            if (windowReady) continue;
            Glfw3.DestroyWindow(window);
            return;
        }

        windowReady = false;
    }

    private void SizeCallback(WindowHandle windowHandle, int width, int height)
    {
        Canvas.Height = (uint)height;
        Canvas.Width = (uint)width;

        lastResize = DateTime.Now;
    }

    private void MouseCallback(WindowHandle windowHandle, double xPosition, double yPosition)
    {
        var mousePos = new Vector2((float)xPosition, (float)yPosition);

        if (mousePos == previousMousePosition)
            return;

        OnMouseMove?.Invoke(this, new AjivaMouseMotionCallbackEventArgs(mousePos, -(previousMousePosition - mousePos), activeLayer));

        previousMousePosition = mousePos;
    }

    private void KeyCallback(WindowHandle windowHandle, Key key, int scancode, InputAction inputAction, Modifier modifiers)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault.
        switch (key)
        {
            //todo dev only
            case Key.Escape:
                Environment.Exit(0);
                break;
            case Key.Tab when inputAction == InputAction.Press:
                activeLayer = activeLayer switch {
                    AjivaEngineLayer.Layer3d => AjivaEngineLayer.Layer2d,
                    AjivaEngineLayer.Layer2d => AjivaEngineLayer.Layer3d,
                    _ => throw new ArgumentOutOfRangeException()
                };
                UpdateCursor();
                //LogHelper.WriteLine($"activeLayer: {activeLayer}");
                break;
            case Key.F11:
                if (inputAction == InputAction.Press)
                    SharpVk.Glfw.extras.Glfw3.Public.SetWindowAttrib_0(windowHandle.RawHandle, (int)State.Decorated,
                        SharpVk.Glfw.extras.Glfw3.Public.GetWindowAttrib_0(windowHandle.RawHandle, (int)State.Decorated) == (int)State.False
                            ? (int)State.True
                            : (int)State.False);
                //SharpVk.Glfw.extras.Glfw3.Public.SetWindowSize_0(windowHandle.RawHandle, primaryMonitorVideoMode.Width, primaryMonitorVideoMode.Height);
                break;
        }

        OnKeyEvent?.Invoke(this, key, scancode, inputAction, modifiers);
    }

    private void UpdateCursor()
    {
        Glfw3.SetInputMode(window, Glfw3Enum.GLFW_CURSOR, activeLayer == AjivaEngineLayer.Layer3d ? Glfw3Enum.GLFW_CURSOR_DISABLED : Glfw3Enum.GLFW_CURSOR_NORMAL);
    }

    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Canvas.Dispose();
        CloseWindow();
    }
}
public delegate void WindowResizedDelegate(object sender, Extent2D oldSize, Extent2D newSize);
public delegate void KeyEventHandler(object? sender, Key key, int scancode, InputAction inputAction, Modifier modifiers);
public record AjivaMouseMotionCallbackEventArgs(Vector2 Pos, Vector2 Delta, AjivaEngineLayer ActiveLayer);
