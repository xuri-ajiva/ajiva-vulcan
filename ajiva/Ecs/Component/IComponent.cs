using System;

namespace ajiva.Ecs.Component
{
    public interface IComponent : IDisposable
    {
        bool Dirty { get; set; }
    }
}
