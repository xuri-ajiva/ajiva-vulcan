using System;

namespace ajiva.Ecs.System
{
    public interface ISystem : IDisposable
    {
        void Setup(AjivaEcs ecs);
    }
}
