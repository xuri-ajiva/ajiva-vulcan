#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs;

public interface IEntityRegistry
{
    long EntitiesCount { get; }
    void RegisterEntity<T>(T entity) where T : class, IEntity;
    bool TryUnRegisterEntity<T>(uint id, [MaybeNullWhen(false)] out T entity) where T : IEntity;
    bool TryUnRegisterEntity<T>(T entity) where T : IEntity;
}
