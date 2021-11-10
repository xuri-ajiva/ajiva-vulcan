namespace ajiva.Ecs.System;

public abstract class SystemBase : DisposingLogger, ISystem
{
    protected IAjivaEcs Ecs { get; }

    /// <inheritdoc />
    protected SystemBase(IAjivaEcs ecs)
    {
        Ecs = ecs;
    }
}