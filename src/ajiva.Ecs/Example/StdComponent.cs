using System;

namespace ajiva.Ecs.Example;

public class StdComponent : IComponent
{
    public int Health { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{nameof(Health)}: {Health}";
    }


    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}