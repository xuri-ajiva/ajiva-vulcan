using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ajiva.Components;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Entities;
using ajiva.Systems.VulcanEngine.EngineManagers;
using GlmSharp;
using SharpVk;

namespace ajiva.Systems.VulcanEngine.Engine
{
    public interface IRenderEngine : IComponentSystem<ARenderAble>
    {
        Instance? Instance { get; }
        DeviceComponent DeviceComponent { get; }
        SwapChainComponent SwapChainComponent { get; }
        PlatformWindow Window { get; }
        ImageComponent ImageComponent { get; }
        GraphicsComponent GraphicsComponent { get; }
        ShaderComponent ShaderComponent { get; }
        SemaphoreComponent SemaphoreComponent { get; }
        TextureComponent TextureComponent { get; }

        event PlatformEventHandler OnFrame;
        event KeyEventHandler OnKeyEvent;
        event EventHandler OnResize;
        event EventHandler<vec2> OnMouseMove;

        Cameras.Camera MainCamara { get; set; }

        public object RenderLock { get; }
        public object UpdateLock { get; }
        public AjivaEcs Ecs { get; }

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
