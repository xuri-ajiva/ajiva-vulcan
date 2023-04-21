using System.Collections.Concurrent;
using Ajiva.Ecs;
using Autofac;

public class ContainerProxy : DisposingLogger, IEntityRegistry
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
        if (entity is IUpdate update)
            Container.Resolve<IUpdateManager>().RegisterUpdate(update);
    }

    public ConcurrentDictionary<Guid, IEntity> Entities { get; } = new ConcurrentDictionary<Guid, IEntity>();

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool TryUnRegisterEntity<T>(uint id, out T entity) where T : IEntity
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public long EntitiesCount  => Entities.Count;

    public long ComponentsCount { get; set; }

    public IContainer Container { get; set; }
}
