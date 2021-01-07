using System;
using System.Linq.Expressions;
using ajiva.EngineManagers;
using ajiva.Models;
using SharpVk;
using SharpVk.Khronos;

namespace ajiva.Engine
{
    public interface IEngine
    {
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

        public T NotNull<T>(Expression<Func<T?>> action)
        {
            var expression = (MemberExpression)action.Body;
            string name = expression.Member.Name;
            var res = action.Compile()();
            if (res == null)
            {
                throw new TypeInitializationException(typeof(T).FullName, new ArgumentException(name));
            }
            return res;
        }

        public void Dependent<T>(T obj, string name)
        {
            if (obj == null)
            {
                throw new TypeInitializationException(typeof(T).FullName, new ArgumentException(name));
            }
        }
    }
}
