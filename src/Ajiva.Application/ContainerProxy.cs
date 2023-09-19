using System.Collections.Concurrent;
using Ajiva.Ecs;
using Autofac;

namespace Ajiva.Application;

public class ContainerProxy : DisposingLogger, IEntityRegistry, IContainerAccessor
{
    public ConcurrentDictionary<Guid, IEntity> Entities { get; } = new ConcurrentDictionary<Guid, IEntity>();

    public long ComponentsCount { get; set; }

    public IContainer Container { get; set; }

    /// <inheritdoc />
    public void RegisterEntity<T>(T entity) where T : class, IEntity
    {
        while (!Entities.TryAdd(entity.Id, entity)) Thread.Yield();
        if (entity is IUpdate update)
            Container.Resolve<IUpdateManager>().RegisterUpdate(update);
    }

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
    public long EntitiesCount => Entities.Count;

    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        foreach (var entity in Entities.Values.OfType<IDisposable>())
        {
            entity.Dispose();
        }
    }
}
