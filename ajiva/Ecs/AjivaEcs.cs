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
        public bool Available { get; private set; }
        private readonly object @lock = new();
        private uint currentEntityId;
        private Dictionary<uint, IEntity> Entities { get; } = new();
        private Dictionary<Type, IEntityFactory> Factories { get; } = new();
        private Dictionary<Type, IComponentSystem> ComponentSystems { get; } = new();
        private Dictionary<Type, ISystem> Systems { get; } = new();
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

        public void AddComponentSystem(IComponentSystem system)
        {
            Systems.Add(system);
        }

        public List<IComponentSystem> Systems { get; } = new();

        public T CreateComponent<T>(IEntity entityId) where T : class, IComponent
        {
            foreach (var system in Systems.Where(system => system.ComponentType == typeof(T)))
            {
                return (T)system.CreateComponent(entityId);
            }
            throw new ArgumentException($"{typeof(T)} Has No Factory!");
        }

        public void AddEntityFactory(Type type, IEntityFactory entityFactory)
        {
            Factories.Add(type, entityFactory);
        }

        public T GetPara<T>(string name) => (T)Params[name];

        public bool TryGetPara<T>(string name, out T? value)
        {
            value = default;
            if (!Params.TryGetValue(name, out var tmp)) return false;
            value = (T)tmp;
            return true;
        }

        public void AddParam(string name, object data)
        {
            Params.Add(name, data);
        }

        public T GetComponentSystem<T>() => (T)Systems.First(x => x.GetType() == typeof(T));

        public async Task InitSystems()
        {
            Task.WaitAll(Systems.Select(x => Task.Run(() => x.Init(this))).ToArray());

            Available = true;
        }

        public void AttachComponentToEntity<T>(IEntity entity) where T : class, IComponent
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
