namespace ajiva.Ecs;

public static class Exts
{
    public static T Register<T>(this T entity, IAjivaEcs ecs) where T : class, IEntity
    {
        foreach (var component in entity.GetComponents())
        {
            if (component is not null)
            {
                ecs.RegisterComponent(entity, component.GetType(), component);
            }
        }
        ecs.RegisterEntity(entity);
        return entity;
    }
}
