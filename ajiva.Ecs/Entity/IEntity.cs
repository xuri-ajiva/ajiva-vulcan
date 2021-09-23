using System;
using System.Collections.Generic;
using ajiva.Ecs.Component;
using ajiva.Utils;

namespace ajiva.Ecs.Entity
{
    public interface IEntity : IDisposable
    {
        uint Id { get; }
        IDictionary<TypeKey, IComponent> Components { get; }
    }
}
