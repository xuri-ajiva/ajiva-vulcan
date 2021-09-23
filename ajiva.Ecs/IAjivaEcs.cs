#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Factory;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Utils;

namespace ajiva.Ecs
{
    public interface IAjivaEcs : IDisposingLogger
    {
#region Add

        void AddEntityFactory<T>(IEntityFactory<T> entityFactory) where T : class, IEntity;
        void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent;
        void AddParam(string name, object data);
        void AddInstance<T>(T instance) where T : class;
        void AddSystem<T>(T system) where T : class, ISystem;

#endregion

#region Get

        T GetPara<T>(string name);
        bool TryGetPara<T>(string name, [MaybeNullWhen(false)] out T value);
        T GetInstance<T>() where T : class;
        bool TryGetInstance<T>([MaybeNullWhen(false)] out T value) where T : class;

        T GetSystem<T>() where T : class, ISystem;

        IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent;
        TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent;

#endregion

#region Entity

        T CreateEntity<T>() where T : class, IEntity;
        void RegisterEntity<T>(T entity) where T : class, IEntity;

#endregion

#region Component

        T CreateComponent<T>(IEntity entity) where T : class, IComponent;
        T RegisterComponent<T>(IEntity entity, T component) where T : class, IComponent;
        void AttachNewComponentToEntity<T>(IEntity entity) where T : class, IComponent;
        void AttachComponentToEntity<T>(IEntity entity, T component) where T : class, IComponent;

#endregion

#region Create

        T CreateSystemOrComponentSystem<T>() where T : class, ISystem;
        T CreateObjectAndInject<T>(Func<Type, object?> missing) where T : class;

#endregion

#region Delete

        IEntity? DeleteEntity(uint id);

        T RemoveComponent<T>(IEntity entity, T component) where T : class, IComponent;

#endregion

#region Live

        bool Available { get; }
        void InitSystems();
        void Update(UpdateInfo delta);
        void IssueClose();

        void RegisterUpdate(IUpdate update);
        void RegisterInit(IInit init);

#endregion
    }
}
