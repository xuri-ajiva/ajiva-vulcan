﻿using System;
using System.Threading.Tasks;
using ajiva.EngineManagers;
using GlmSharp;
using SharpVk.Khronos;

namespace ajiva.Engine
{
    public interface IPlatformWindow : IDisposable
    {
        public Task InitWindow(int surfaceWidth, int surfaceHeight);
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
