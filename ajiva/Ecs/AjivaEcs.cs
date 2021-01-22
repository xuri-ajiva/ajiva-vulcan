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
        private object Lock = new();
        private uint currentEntityId;
        public uint CurrentEntityId => currentEntityId;
        private Dictionary<uint, IEntity> Entities { get; } = new();
        private Dictionary<Type, IEntityFactory> Factorys { get; } = new();

        public Dictionary<string, object> Params { get; } = new();

        public void Update(TimeSpan delta)
        {
            lock (Lock)
            {
                foreach (var system in Systems)
                {
                    system.Update(delta);
                }

                foreach (var entity in Entities.Values.Where(x => x.HasUpdate))
                {
                    entity.Update(delta);
                }
            }
        }

        public T CreateEntity<T>() where T : class, IEntity
        {
            foreach (var factory in Factorys.Where(factory => factory.Key == typeof(T)))
            {
                lock (Lock)
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
            Factorys.Add(type, entityFactory);
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
            /*
                        var pl = Parallel.ForEach(Systems, async system => await system.Init(this));
                        */

            foreach (var system in Systems)
            {
                await system.Init(this);
            }
            Available = true;
        }

        public void AttachComponentToEntity<T>(IEntity entity) where T : class, IComponent
        {
            foreach (var system in Systems.Where(system => system.ComponentType == typeof(T)))
            {
                system.AttachNewComponent(entity);
            }
        }

        public void IssueClose()
        {
            lock (Lock)
            {
                Available = false;
                Task.Run(ReleaseUnmanagedResources);
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            lock (Lock)
            {
                foreach (var factory in Factorys)
                {
                    factory.Value?.Dispose();
                }
                Factorys.Clear();

                foreach (var entity in Entities)
                {
                    entity.Value.Dispose();
                }
                Entities.Clear();

                foreach (var system in Systems)
                {
                    system?.Dispose();
                }
                Systems.Clear();
            }
        }
    }
}
