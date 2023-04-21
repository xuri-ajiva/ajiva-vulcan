namespace Ajiva.Ecs.ComponentSytem;

public interface IComponentSystem : ISystem
{
    Type ComponentType { get; }
    IComponent RegisterComponent(IEntity entity, IComponent component);
    IComponent UnRegisterComponent(IEntity entity, IComponent component);
}
public interface IComponentSystem<T> : IComponentSystem where T : IComponent
{
    Dictionary<T, IEntity> ComponentEntityMap { get; }

    T RegisterComponent(IEntity entity, T component);
    T UnRegisterComponent(IEntity entity, T component);
    T CreateComponent(IEntity entity);
    void DeleteComponent(T? component);
}