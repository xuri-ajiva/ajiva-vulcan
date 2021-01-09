using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ajiva.EngineManagers;
using SharpVk;

namespace ajiva.Engine
{
    public interface IEngine
    {
        public bool Runing { get; }

        Instance? Instance { get; }
        DeviceManager DeviceManager { get; }
        SwapChainManager SwapChainManager { get; }
        IPlatformWindow Window { get; }
        ImageManager ImageManager { get; }
        GraphicsManager GraphicsManager { get; }
        ShaderManager ShaderManager { get; }
        BufferManager BufferManager { get; }
        SemaphoreManager SemaphoreManager { get; }
        TextureManager TextureManager { get; }

#pragma warning disable 8763
        [DoesNotReturn, MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public IEngine Reacquiring<T>(Expression<Func<T?>> path, bool required)
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
