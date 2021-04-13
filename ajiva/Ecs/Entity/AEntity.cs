using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ajiva.Ecs.Component;
using ajiva.Utils;

namespace ajiva.Ecs.Entity
{
    public abstract class AEntity : DisposingLogger, IEntity
    {
        public static uint CurrentId { get; [MethodImpl(MethodImplOptions.Synchronized)] private set; }

        /// <inheritdoc />
        public uint Id { get; init; } = CurrentId++;

        public IDictionary<TypeKey, IComponent> Components { get; } = new Dictionary<TypeKey, IComponent>();
    }
}
