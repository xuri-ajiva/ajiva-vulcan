using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Factory;
using ajiva.Helpers;

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

        public void AddEntityFactory(Type type, IEntityFactory entityFactory) => Factories.Add(type, entityFactory);

        public void AddComponentSystem<T>(IComponentSystem<T> system) where T : class, IComponent => ComponentSystems.Add(typeof(T), system);
        public IComponentSystem<T> GetComponentSystemByComponent<T>() where T : class, IComponent => (IComponentSystem<T>)ComponentSystems[typeof(T)];
        public TS GetComponentSystem<TS, TC>() where TS : IComponentSystem<TC> where TC : class, IComponent => (TS)ComponentSystems[typeof(TC)];

        public void AddSystem<T>(T system) where T : class, ISystem
        {
            if (system is IComponentSystem)
                throw new ArgumentException("IComponentSystem should not be assinged as ISystem");
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
            Instances.Add(typeof(T), Instances);
        }

        public T GetInstance<T>() where T : class => (T)Instances[typeof(T)];

        public void InitSystems()
        {
            Init(InitPhase.Start);
            Init(InitPhase.PreInit);
            Init(InitPhase.Init);
            Init(InitPhase.PreMain);
            Init(InitPhase.Main);
            Init(InitPhase.PostMain);
            Init(InitPhase.Post);
            Init(InitPhase.Finish);
            Available = true;
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

        private void Init(InitPhase phase)
        {
            if (!Inits.ContainsKey(phase)) return;
            lock (@lock)
                if (multiThreading)
                    Parallel.ForEach(Inits[phase], i => i.Init(this, phase));
                else
                    foreach (var init in Inits[phase])
                        init.Init(this, phase);
        }

        public void Update(TimeSpan delta)
        {
            if (Updates.Count < 1) return;
            lock (@lock)
                if (multiThreading)
                    Parallel.ForEach(Updates, d => d.Update(delta));
                else
                    foreach (var update in Updates)
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

        private List<IUpdate> Updates = new();

        public void RegisterUpdate(IUpdate update) => Updates.Add(update);

        private Dictionary<InitPhase, List<IInit>> Inits = new();

        public void RegisterInit(IInit init, InitPhase phase)
        {
            if (Inits.ContainsKey(phase))
                Inits[phase].Add(init);
            else
                Inits.Add(phase, new() {init});
        }
    }
}
