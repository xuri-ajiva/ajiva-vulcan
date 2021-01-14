﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ajiva.EngineManagers;
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
        AEnittyComponent AEnittyComponent { get; }
        SemaphoreComponent SemaphoreComponent { get; }
        TextureComponent TextureComponent { get; }
        public object Lock { get; }

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