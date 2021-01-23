using System;
using ajiva.Ecs.Component;

namespace ajiva.Ecs.Example
{
    public class StdComponent : IComponent
    {
        public int Health { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{nameof(Health)}: {Health}";
        }

        /// <inheritdoc />
        public bool Dirty { get; set; }


        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
