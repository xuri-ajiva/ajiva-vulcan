namespace ajiva.Ecs;

public static class IAjivaEcsExtensions
{
    public static bool TryAttachComponentToEntity<T>(this IAjivaEcs ecs, IEntity entity, T component) where T : class, IComponent
    {
        return ecs.TryAttachComponentToEntity<T, T>(entity, component);
    }

    public static T AddComponent<T>(this IEntity entity, T component) where T : class, IComponent
    {
        return entity.AddComponent<T, T>(component);
    }
}
