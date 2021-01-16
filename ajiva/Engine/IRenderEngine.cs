using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ajiva.EngineManagers;
using GlmSharp;
using SharpVk;

namespace ajiva.Engine
{
    public interface IRenderEngine
    {
        public bool Runing { get; }

        Instance? Instance { get; }
        DeviceComponent DeviceComponent { get; }
        SwapChainComponent SwapChainComponent { get; }
        PlatformWindow Window { get; }
        ImageComponent ImageComponent { get; }
        GraphicsComponent GraphicsComponent { get; }
        ShaderComponent ShaderComponent { get; }
        AEntityComponent AEntityComponent { get; }
        SemaphoreComponent SemaphoreComponent { get; }
        TextureComponent TextureComponent { get; }
        
        event PlatformEventHandler OnFrame;
        event PlatformEventHandler OnUpdate;
        event KeyEventHandler OnKeyEvent;
        event EventHandler OnResize;
        event EventHandler<vec2> OnMouseMove;
        
        Cameras.Camera MainCamara { get; set; }

        public object RenderLock { get; }
        public object UpdateLock { get; }

#pragma warning disable 8763
        [DoesNotReturn, MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public IRenderEngine Reacquiring<T>(Expression<Func<T?>> path, bool required)
        {
            var expression = (MemberExpression)path.Body;
            string name = expression.Member.Name;
            var res = path.Compile()();

            switch (required)
            {
                case true when res == null:
                    throw new NullReferenceException(typeof(T).FullName, new ArgumentException(name));
                case false when res != null:
                    throw new TypeInitializationException(typeof(T).FullName, new ArgumentException(name));
            }
            return this;
        }
#pragma warning restore 8763

        public void Dependent<T>(T obj, string name)
        {
            if (obj == null)
            {
                throw new TypeInitializationException(typeof(T).FullName, new ArgumentException(name));
            }
        }
    }
}
