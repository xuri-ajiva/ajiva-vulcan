using System;
using System.Collections.Generic;

namespace ajiva.Ecs.Entity;

public interface IEntity : IDisposable
{
    uint Id { get; }
    IDictionary<TypeKey, IComponent> Components { get; }
}