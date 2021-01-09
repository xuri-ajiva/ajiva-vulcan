using System;
using System.Collections.Generic;
using GlmSharp;
using SharpVk.Khronos;

namespace ajiva.EngineManagers
{
    public interface IPlatformWindow : IDisposable
    {
        public void InitWindow(int surfaceWidth, int surfaceHeight);
        public IEnumerable<string> GetRequiredInstanceExtensions();
        public void CreateSurface();
        public void MainLoop(TimeSpan timeToRun);
        public void CloseWindow();
        public event PlatformEventHandler OnFrame;
        public event KeyEventHandler OnKeyEvent;
        public event EventHandler OnResize;
        public event EventHandler<vec2> OnMouseMove;

        public Surface Surface { get; }
        uint SurfaceWidth { get; }
        uint SurfaceHeight { get; }
    }
}