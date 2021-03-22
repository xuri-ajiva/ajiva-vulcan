using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ajiva.Ecs.Component;
using ajiva.Ecs.ComponentSytem;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Factory;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Helpers;
using ajiva.Systems.VulcanEngine.Systems;

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
        private Dictionary<Type, IEntityFactory> Factories { get; } = new();
        private Dictionary<Type, IComponentSystem> ComponentSystems { get; } = new();
        private Dictionary<Type, ISystem> Systems { get; } = new();
        private Dictionary<Type, object> Instances { get; } = new();
        private Dictionary<string, object> Params { get; } = new();

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

        public T CreateComponent<T>(IEntity entity) where T : class, IComponent => ((IComponentSystem<T>)ComponentSystems[typeof(T)]).CreateComponent(entity);

        public void AttachComponentToEntity<T>(IEntity entity) where T : class, IComponent => ((IComponentSystem<T>)ComponentSystems[typeof(T)]).AttachNewComponent(entity);

        public void AddEntityFactory<T>(IEntityFactory<T> entityFactory) where T : class, IEntity => Factories.Add(typeof(T), entityFactory);

        public void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent => ComponentSystems.Add(typeof(T), system);
        public IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent => (IComponentSystem<T>)ComponentSystems[typeof(T)];
        public TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent => (TS)ComponentSystems[typeof(TC)];

        public void AddSystem<T>(T system) where T : class, ISystem
        {
            if (system is IComponentSystem)
                throw new ArgumentException("IComponentSystem should not be assigned as ISystem");
            Systems.Add(typeof(T), system);
        }

        public T GetSystem<T>() where T : class, ISystem => (T)Systems[typeof(T)];

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
            Instances.Add(typeof(T), instance);
        }

        public T GetInstance<T>() where T : class => (T)Instances[typeof(T)];

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
                            Systems.Add(nb.GetType(), (ISystem)nb);
                        InitOne(nb, initDone);
                    }
                }
            }
            LogHelper.Log($"Init: {toInit.GetType()}");
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
        protected override void ReleaseUnmanagedResources()
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

                foreach (var system in ComponentSystems.Values)
                {
                    system?.Dispose();
                }
                ComponentSystems.Clear();

                foreach (var system in Systems.Values)
                {
                    system?.Dispose();
                }
                Systems.Clear();
            }
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
