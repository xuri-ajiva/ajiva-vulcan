using System;
using ajiva.Ecs.Entity;

namespace ajiva.Entitys
{
    public class DefaultEntity : AEntity
    {
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }

        /// <inheritdoc />
        public override void Update(TimeSpan delta)
        {
        }
    }
}
