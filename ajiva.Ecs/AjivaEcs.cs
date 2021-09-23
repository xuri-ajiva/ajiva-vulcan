﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Factory;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Utils;
using Ajiva.Wrapper.Logger;
using Microsoft.CSharp.RuntimeBinder;

namespace ajiva.Ecs
{
    public class AjivaEcs : DisposingLogger, IAjivaEcs
    {
        private readonly bool multiThreading;

        public AjivaEcs(bool multiThreading)
        {
            this.multiThreading = multiThreading;
        }

        /// <inheritdoc />
        public bool Available { get; private set; }

        private readonly object @lock = new object();
        private uint currentEntityId;
        public Dictionary<uint, IEntity> Entities { get; } = new Dictionary<uint, IEntity>();

        public Dictionary<Type, IEntityFactory> Factories { get; } = new();

        public Dictionary<Type, IComponentSystem> ComponentSystems { get; } = new();

        public Dictionary<Type, ISystem> Systems { get; } = new();

        public Dictionary<Type, object> Instances { get; } = new();

        public Dictionary<string, object> Params { get; } = new Dictionary<string, object>();
        private static readonly Type Me = typeof(AjivaEcs);

#region Add

        /// <inheritdoc />
        public void RegisterEntity<T>(T entity) where T : class, IEntity
        {
            Entities.Add(entity.Id, entity);
        }

        /// <inheritdoc />
        public T RegisterComponent<T>(IEntity entity, T component) where T : class, IComponent
        {
            return GetComponentSystemByComponent<T>().RegisterComponent(entity, component);
        }

        /// <inheritdoc />
        public void AttachNewComponentToEntity<T>(IEntity entity) where T : class, IComponent
        {
            entity.AddComponent(CreateComponent<T>(entity));
        }

        /// <inheritdoc />
        public void AttachComponentToEntity<T>(IEntity entity, T component) where T : class, IComponent
        {
            entity.AddComponent(component);
            RegisterComponent(entity, component);
        }

        /// <inheritdoc />
        public void AddEntityFactory<T>(IEntityFactory<T> entityFactory) where T : class, IEntity
        {
            Factories.Add(typeof(T), entityFactory);
        }

        /// <inheritdoc />
        public void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent
        {
            ComponentSystems.Add(system.GetType(), system);
        }

        /// <inheritdoc />
        public void AddSystem<T>(T system) where T : class, ISystem
        {
            if (system is IComponentSystem)
                throw new ArgumentException("IComponentSystem should not be assigned as ISystem");
            Systems.Add(system.GetType(), system);
        }

        /// <inheritdoc />
        public void AddInstance<T>(T instance) where T : class
        {
            Instances.Add(instance.GetType(), instance);
        }

        /// <inheritdoc />
        public void AddParam(string name, object data)
        {
            Params.Add(name, data);
        }

#endregion

#region Get

        /// <inheritdoc />
        public IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent
        {
            return (IComponentSystem<T>)ComponentSystems[typeof(T)];
        }

        /// <inheritdoc />
        public TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent
        {
            return (TS)ComponentSystems[typeof(TC)];
        }

        /// <inheritdoc />
        public T GetSystem<T>() where T : class, ISystem
        {
            return (T)Systems[typeof(T)];
        }

        /// <inheritdoc />
        public T GetPara<T>(string name)
        {
            return (T)Params[name];
        }

        /// <inheritdoc />
        public bool TryGetPara<T>(string name, [MaybeNullWhen(false)] out T value)
        {
            value = default;
            if (!Params.TryGetValue(name, out var tmp)) return false;
            value = (T)tmp;
            return true;
        }

        /// <inheritdoc />
        public T GetInstance<T>() where T : class
        {
            return (T)Instances[typeof(T)];
        }

        /// <inheritdoc />
        public bool TryGetInstance<T>([MaybeNullWhen(false)] out T value) where T : class
        {
            value = default;
            if (!Instances.TryGetValue(typeof(T), out var tmp)) return false;
            value = (T)tmp;
            return true;
        }

#endregion

#region Create

        /// <inheritdoc />
        public T CreateSystemOrComponentSystem<T>() where T : class, ISystem
        {
            return (T)CreateSystemOrComponentSystemIfNotExitsRecursive(typeof(T));
        }

        private object CreateSystemOrComponentSystemIfNotExitsRecursive(Type type)
        {
            if (type.FindInterfaces((tpe, _) => tpe == typeof(ISystem) && tpe != typeof(IComponentSystem<>), null).Any())
                if (Systems.ContainsKey(type))
                    return Systems[type];

            var instance = CreateObjectAndInject(type, missing =>
            {
                var iFaces = missing.GetInterfaces();
                if (iFaces.Contains(typeof(ISystem)) && !iFaces.Contains(typeof(IComponentSystem)))
                {
                    return CreateSystemOrComponentSystemIfNotExitsRecursive(missing);
                }
                LogHelper.Log($"Error Cannot instantiate {missing}!");
                return null;
            });

            switch (instance)
            {
                case IComponentSystem componentSystem:
                    if (ComponentSystems.ContainsKey(componentSystem.ComponentType))
                    {
                        LogHelper.Log($"Dup Component System Creation: {componentSystem.GetHashCode()}");
                        componentSystem.Dispose();
                        return ComponentSystems[componentSystem.ComponentType];
                    }
                    ComponentSystems.Add(componentSystem.ComponentType, componentSystem);
                    break;
                case ISystem system:
                    AddSystem(system);
                    break;
                default:
                    throw new RuntimeBinderInternalCompilerException("The Compiler has Failed on the T constrain");
            }

            if (instance is IUpdate update)
                RegisterUpdate(update);
            if (instance is IInit init)
                RegisterInit(init);

            return instance;
        }

        /// <inheritdoc />
        public T CreateObjectAndInject<T>(Func<Type, object?> missing) where T : class
        {
            return (T)CreateObjectAndInject(typeof(T), missing);
        }

        private object CreateObjectAndInject(Type type, Func<Type, object?> missing)
        {
            object instance;
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (constructors.FirstOrDefault(x =>
                x.GetParameters() is { Length: >= 1 } parameters
                && parameters.Any(y =>
                    y.ParameterType.IsAssignableFrom(Me)
                )) is { } ctr)
            {
                var para = ctr.GetParameters();
                object?[] args = new object?[para.Length];
                for (var i = 0; i < para.Length; i++)
                {
                    var key = para[i].ParameterType;
                    if (key.IsAssignableFrom(Me))
                    {
                        args[i] = this;
                        continue;
                    }

                    bool ContainsArgTypeAndFill<TD>(IDictionary<Type, TD> dict) where TD : class
                    {
                        if (dict.SingleOrDefault(x => x.Key.IsAssignableTo(key)) is not TD inject) return false;
                        args[i] = inject;
                        return true;
                    }

                    if (ContainsArgTypeAndFill(ComponentSystems))
                        continue;
                    if (ContainsArgTypeAndFill(Instances))
                        continue;
                    if (ContainsArgTypeAndFill(Systems))
                        continue;

                    if (args[i] is not null)
                        continue;

                    args[i] = missing(para[i].ParameterType);

                    /*if (args[i] is not null)
                        continue;
                    
                    LogHelper.Log($"Creating New Instance of {para[i].ParameterType} for constructor of {type}");
                    args[i] = Activator.CreateInstance(para[i].ParameterType);*/
                }
                instance = ctr.Invoke(args);
            }
            else if (constructors.FirstOrDefault(x => x.GetParameters().Length == 0) is { } ctr2)
                instance = ctr2.Invoke(null);
            else
                instance = constructors.First().Invoke(Array.Empty<object?>());
            return instance;
        }

        /// <inheritdoc />
        public T CreateComponent<T>(IEntity entity) where T : class, IComponent
        {
            return ((IComponentSystem<T>)ComponentSystems[typeof(T)]).CreateComponent(entity);
        }

        /// <inheritdoc />
        public T CreateEntity<T>() where T : class, IEntity
        {
            foreach (var factory in Factories.Where(factory => factory.Key == typeof(T)))
            {
                lock (@lock)
                {
                    var entity = factory.Value.Create(this, currentEntityId++);
                    Entities.Add(entity.Id, entity);
                    return (T)entity;
                }
            }
            return default!;
        }

#endregion

#region Delete

        public IEntity? DeleteEntity(uint id)
        {
            if (!Entities.TryGetValue(id, out var entity)) return default;

            foreach (var (_, value) in entity.Components)
            {
                RemoveComponent(entity, value);
            }
            return entity;
        }

        /// <inheritdoc />
        public T RemoveComponent<T>(IEntity entity, T component) where T : class, IComponent
        {
            return ((IComponentSystem<T>)ComponentSystems[typeof(T)]).RemoveComponent(entity, component);
        }

#endregion

#region Live

        /// <inheritdoc />
        public void InitSystems()
        {
            List<IInit> isInti = new List<IInit>();
            foreach (var init in inits.ToArray())
            {
                InitOne(init, isInti);
            }

            Available = true;
        }

        /// <inheritdoc />
        private void InitOne(IInit toInit, ICollection<IInit> initDone)
        {
            if (initDone.Contains(toInit)) return;
            var attrib = toInit.GetType().GetCustomAttributes(typeof(DependentAttribute), false).FirstOrDefault();

            if (attrib is DependentAttribute dependent)
            {
                foreach (var type in dependent.Dependent)
                {
                    var typeInterfaces = type.GetInterfaces();
                    if (!typeInterfaces.Any(x => x == typeof(IInit))) continue;

                    var deps = inits.Where(x => x.GetType() == type).ToArray();
                    if (deps.Any())
                    {
                        foreach (var dep in deps)
                            if (!initDone.Contains(dep))
                                InitOne(dep, initDone);
                    }
                    else
                    {
                        var nb = (IInit)Activator.CreateInstance(type)!;
                        //add it to the list of init systems
                        inits.Add(nb);
                        // first check IComponentSystem because it inherits from ISystem
                        if (typeInterfaces.Any(x => x == typeof(IComponentSystem)))
                            ComponentSystems.Add(((IComponentSystem)nb).ComponentType, (IComponentSystem)nb);
                        //last check in an else if the type inherits the heights interface in the hierarchy
                        else if (typeInterfaces.Any(x => x == typeof(ISystem)))
                            Systems.Add(nb.GetType(), (ISystem)nb);
                        InitOne(nb, initDone);
                    }
                }
            }
            //LogHelper.Log($"Init: {toInit.GetType()}");
            toInit.Init(this);
            initDone.Add(toInit);
        }

        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (updates.Count < 1) return;
            lock (@lock)
                if (multiThreading)
                    Parallel.ForEach(updates, d => d.Update(delta));
                else
                    foreach (var update in updates)
                        update.Update(delta);
        }

        /// <inheritdoc />
        public void IssueClose()
        {
            lock (@lock) Available = false;
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            lock (@lock)
            {
                foreach (var factory in Factories.Values)
                {
                    factory.Dispose();
                }
                Factories.Clear();

                foreach (var entity in Entities.Values)
                {
                    entity.Dispose();
                }
                Entities.Clear();

                IDictionary<Type, ISystem> disposingSet = new Dictionary<Type, ISystem>();
                foreach (var (key, value) in ComponentSystems)
                {
                    disposingSet.Add(key, value);
                }
                foreach (var (key, value) in Systems)
                {
                    disposingSet.Add(key, value);
                }
                foreach (var (_, value) in disposingSet)
                {
                    if (!value.Disposed)
                    {
                        DisposeRec(value, disposingSet);
                    }
                }

                ComponentSystems.Clear();
                Systems.Clear();
            }
        }

        /// <inheritdoc />
        private static void DisposeRec(ISystem toDispose, IDictionary<Type, ISystem> disposingSet)
        {
            foreach (var (_, system) in disposingSet)
            {
                var attrib = system.GetType().GetCustomAttributes(typeof(DependentAttribute), false).FirstOrDefault();
                if (attrib is not DependentAttribute dependent) continue;
                if (!dependent.Dependent.Any(x => x == toDispose.GetType())) continue;

                DisposeRec(system, disposingSet);
            }
            toDispose.Dispose();
        }

        private readonly List<IUpdate> updates = new List<IUpdate>();

        /// <inheritdoc />
        public void RegisterUpdate(IUpdate update)
        {
            lock (regLock)
                updates.Add(update);
        }

        private readonly List<IInit> inits = new List<IInit>();

        private readonly object regLock = new object();

        /// <inheritdoc />
        public void RegisterInit(IInit init)
        {
            //LogHelper.WriteLine(init.GetHashCode() + " <- "+ phase);
            lock (regLock)
                inits.Add(init);
        }

#endregion
    }
}