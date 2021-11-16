namespace ajiva.Ecs.System;

public abstract class SystemBase : DisposingLogger, ISystem
{
    /// <inheritdoc />
    protected SystemBase(IAjivaEcs ecs)
    {
        Ecs = ecs;
    }

    protected IAjivaEcs Ecs { get; }
}