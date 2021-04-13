using System;
using System.Collections;
using System.Collections.Generic;
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
    public class AjivaEcs : DisposingLogger
    {
        private readonly bool multiThreading;

        public AjivaEcs(bool multiThreading)
        {
            this.multiThreading = multiThreading;
        }

        public bool Available { get; private set; }
        private readonly object @lock = new();
        private uint currentEntityId;
        private Dictionary<uint, IEntity> Entities { get; } = new();
        private Dictionary<TypeKey, IEntityFactory> Factories { get; } = new();
        private Dictionary<TypeKey, IComponentSystem> ComponentSystems { get; } = new();
        private Dictionary<TypeKey, ISystem> Systems { get; } = new();
        private Dictionary<TypeKey, object> Instances { get; } = new();
        private Dictionary<string, object> Params { get; } = new();

        public T CreateEntity<T>() where T : class, IEntity
        {
            foreach (var factory in Factories.Where(factory => factory.Key == UsVc<T>.Key))
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

        public T CreateComponent<T>(IEntity entity) where T : class, IComponent => ((IComponentSystem<T>)ComponentSystems[UsVc<T>.Key]).CreateComponent(entity);

        public void AttachComponentToEntity<T>(IEntity entity) where T : class, IComponent => ((IComponentSystem<T>)ComponentSystems[UsVc<T>.Key]).AttachNewComponent(entity);

        public void AddEntityFactory<T>(IEntityFactory<T> entityFactory) where T : class, IEntity => Factories.Add(UsVc<T>.Key, entityFactory);

        public void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent => ComponentSystems.Add(UsVc<T>.Key, system);
        public IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent => (IComponentSystem<T>)ComponentSystems[UsVc<T>.Key];
        public TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent => (TS)ComponentSystems[UsVc<TC>.Key];

        public void AddSystem<T>(T system) where T : class, ISystem
        {
            if (system is IComponentSystem)
                throw new ArgumentException("IComponentSystem should not be assigned as ISystem");
            Systems.Add(UsVc<T>.Key, system);
        }

        private static readonly TypeKey Me = UsVc<AjivaEcs>.Key;

        public T CreateSystemOrComponentSystem<T>() where T : class, ISystem
        {
            /*if (typeof(T).FindInterfaces((type, _) => type == typeof(IComponentSystem), null).Length != 0)
                throw new ArgumentException("IComponentSystem should not be assigned as ISystem");*/

            object instance;
            var constructors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (constructors.FirstOrDefault(x => x.GetParameters() is {Length: >= 1} parameters && parameters.Any(x => x.ParameterType == typeof(AjivaEcs))) is { } ctr)
            {
                var para = ctr.GetParameters();
                object?[] args = new object?[para.Length];
                for (var i = 0; i < para.Length; i++)
                {
                    var key = UsVc.TypeKeyForType(para[i].ParameterType);
                    if (key == Me)
                    {
                        args[i] = this;
                        continue;
                    }

                    bool ContainsArgType<TD>(IDictionary<TypeKey, TD> dict) where TD : class
                    {
                        if (!dict.ContainsKey(key)) return false;
                        args[i] = dict[key];
                        return true;
                    }

                    if (ContainsArgType(ComponentSystems))
                        continue;
                    if (ContainsArgType(Instances))
                        continue;
                    if (ContainsArgType(Systems))
                        continue;

                    if (args[i] is not null)
                        continue;

                    LogHelper.Log($"Creating New Instance of {para[i].ParameterType} for constructor of {typeof(T)}");
                    args[i] = Activator.CreateInstance(para[i].ParameterType);
                }
                instance = ctr.Invoke(args);
            }
            else if (constructors.FirstOrDefault(x => x.GetParameters().Length == 0) is { } ctr2)
                instance = ctr2.Invoke(null);
            else
                instance = constructors.First().Invoke(Array.Empty<object?>());

            switch (instance)
            {
                case IComponentSystem componentSystem:
                    ComponentSystems.Add(componentSystem.ComponentType, componentSystem);
                    break;
                case ISystem system:
                    Systems.Add(UsVc<T>.Key, system);
                    break;
                default:
                    throw new RuntimeBinderInternalCompilerException("The Compiler has Failed on the T constrain");
            }
            return instance as T;
        }

        public T GetSystem<T>() where T : class, ISystem => (T)Systems[UsVc<T>.Key];

        public T GetPara<T>(string name) => (T)Params[name];

        public bool TryGetPara<T>(string name, out T? value)
        {
            value = default;
            if (!Params.TryGetValue(name, out var tmp)) return false;
            value = (T)tmp;
            return true;
        }

        public void AddParam(string name, object data) => Params.Add(name, data);

        public void AddInstance<T>(T instance) where T : class
        {
            Instances.Add(UsVc<T>.Key, instance);
        }

        public T GetInstance<T>() where T : class => (T)Instances[UsVc<T>.Key];

        public void InitSystems()
        {
            List<IInit> isInti = new();
            foreach (var init in inits.ToArray())
            {
                InitOne(init, isInti);
            }

            Available = true;
        }

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
                            Systems.Add(UsVc.TypeKeyFor(nb), (ISystem)nb);
                        InitOne(nb, initDone);
                    }
                }
            }
            //LogHelper.Log($"Init: {toInit.GetType()}");
            toInit.Init(this);
            initDone.Add(toInit);
        }

        public void SetupSystems()
        {
            lock (@lock)
                if (multiThreading)
                    Parallel.Invoke(
                        () => Parallel.ForEach(ComponentSystems.Values, s => s.Setup(this)),
                        () => Parallel.ForEach(Systems.Values, s => s.Setup(this))
                    );
                else
                {
                    foreach (var system in ComponentSystems.Values) system.Setup(this);
                    foreach (var system in Systems.Values) system.Setup(this);
                }
        }

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
                    factory?.Dispose();
                }
                Factories.Clear();

                foreach (var entity in Entities.Values)
                {
                    entity.Dispose();
                }
                Entities.Clear();

                IDictionary<TypeKey, ISystem> disposingSet = new Dictionary<TypeKey, ISystem>();
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

        private static void DisposeRec(ISystem toDispose, IDictionary<TypeKey, ISystem> disposingSet)
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

        private readonly List<IUpdate> updates = new();

        public void RegisterUpdate(IUpdate update)
        {
            lock (regLock)
                updates.Add(update);
        }

        private readonly List<IInit> inits = new();

        private readonly object regLock = new();

        public void RegisterInit(IInit init)
        {
            //LogHelper.WriteLine(init.GetHashCode() + " <- "+ phase);
            lock (regLock)
                inits.Add(init);
        }
    }
}
