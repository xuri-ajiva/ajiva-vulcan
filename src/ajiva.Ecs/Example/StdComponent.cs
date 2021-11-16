using System;

namespace ajiva.Ecs.Example;

public class StdComponent : IComponent
{
    public int Health { get; set; }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Health)}: {Health}";
    }
}