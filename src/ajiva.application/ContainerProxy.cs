using System.Collections.Concurrent;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Utils;
using Autofac;

public class ContainerProxy : DisposingLogger
{
    /*public TAs Get<T, TAs>() where T : class, IAjivaEcsObject where TAs : IAjivaEcsObject
    {
        //ALog.Warn($"Obsolete Resolve<{typeof(T).Name},{typeof(TAs).Name}>", 3);
        var t = Container.Resolve<T>();
        if (t is TAs tAs)
            return tAs;
        return Container.Resolve<TAs>();
    }*/

    /// <inheritdoc />
    public void RegisterEntity<T>(T entity) where T : class, IEntity
    {
        while (!Entities.TryAdd(entity.Id, entity))
        {
            Thread.Yield();
        }
        if(entity is IUpdate update)
            RegisterUpdate(update);
    }

    public bool TryUnRegisterEntity<T>(uint id, out T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    public ConcurrentDictionary<Guid, IEntity> Entities { get; } = new();

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public T RegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent
    {
        return Container.RegisterComponent(entity, type, component);
    }

    /// <inheritdoc />
    public T UnRegisterComponent<T>(IEntity entity, Type type, T component) where T : class, IComponent
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public long EntitiesCount { get; set; }

    /// <inheritdoc />
    public long ComponentsCount { get; set; }

    public IContainer Container { get; set; }

    /// <inheritdoc />
    public void IssueClose()
    {
        throw new NotImplementedException();
    }

    public List<IUpdate> _updates = new();

    /// <inheritdoc />
    public void RegisterUpdate(IUpdate update)
    {
        //throw new NotImplementedException();
        Console.WriteLine($"RegisterUpdate: {update.GetType()}");
        _updates.Add(update);
    }

    /// <inheritdoc />
    public void StartUpdates()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void WaitForExit()
    {
        throw new NotImplementedException();
    }

    /*public T CreateAndRegisterEntity<T>() where T : class, IEntity
    {
        var entity = Container.ResolveUnregistered<T>();
        entity.Register(Container);
        return entity;
    }*/
}
