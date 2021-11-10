#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace ajiva.Ecs;

public interface IAjivaEcs : IDisposingLogger
{
#region Add

        void AddEntityFactory<T>(IEntityFactory<T> entityFactory) where T : class, IEntity;
        void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent;
        void AddParam(string name, object? data);
        void AddInstance<T>(T instance) where T : class;
        void AddSystem<T>(T system) where T : class, ISystem;

#endregion

#region Get

        [Obsolete("Use Try Get Param")]
        T GetPara<T>(string name);
        bool TryGetPara<T>(string name, [MaybeNullWhen(false)] out T value);
        T GetInstance<T>() where T : class;
        bool TryGetInstance<T>([MaybeNullWhen(false)] out T value) where T : class;

        T GetSystem<T>() where T : class, ISystem;

        IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent;
        TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent;
        TS GetComponentSystemUnSave<TS>() where TS : IComponentSystem;

#endregion

#region Entity

        bool TryCreateEntity<T>([MaybeNullWhen(false)] out T entity) where T : class, IEntity;
        void RegisterEntity<T>(T entity) where T : class, IEntity;

#endregion

#region Component

        bool TryCreateComponent<T>(IEntity entity, [MaybeNullWhen(false)] out T component) where T : class, IComponent;
        T RegisterComponent<T>(IEntity entity, T component) where T : class, IComponent;
        bool TryAttachNewComponentToEntity<T>(IEntity entity, [MaybeNullWhen(false)] out T component) where T : class, IComponent;
        bool TryAttachComponentToEntity<T>(IEntity entity, T component) where T : class, IComponent;
        bool TryDetachComponentFromEntityAndDelete<T>(IEntity entity) where T : class, IComponent;
        bool TryDetachComponentFromEntity<T>(IEntity entity, [MaybeNullWhen(false)] out T component) where T : class, IComponent;

#endregion

#region Create

        T CreateSystemOrComponentSystem<T>() where T : class, ISystem;
        T CreateObjectAndInject<T>(Func<Type, object?> missing) where T : class;

#endregion

#region Delete

        bool TryDeleteEntity(uint id, [MaybeNullWhen(false)] out IEntity entity);
        bool TryUnRegisterEntity(uint id, [MaybeNullWhen(false)] out IEntity entity);

        T UnRegisterComponent<T>(IEntity entity, T component) where T : class, IComponent;
        IEntity DeleteComponent<T>(IEntity entity, T component) where T : class, IComponent;

#endregion

#region Live

#region Count

        long EntitiesCount { get; }
        long ComponentsCount { get; }

#endregion
        bool Available { get; }
        void InitSystems();
        void Update(UpdateInfo delta);
        void IssueClose();

        void RegisterUpdate(IUpdate update);
        void RegisterInit(IInit init);

#endregion
}
