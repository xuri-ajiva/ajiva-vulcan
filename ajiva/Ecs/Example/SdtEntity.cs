using System;
using ajiva.Ecs.Entity;
using ajiva.Helpers;

namespace ajiva.Ecs.Example
{
    public class SdtEntity : AEntity
    {
        /// <inheritdoc />
        public override void Update(UpdateInfo delta)
        {
            if (GetComponent<StdComponent>() is { } health)
            {
                health.Health += new Random().Next(-10, 10);
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }
    };
}
