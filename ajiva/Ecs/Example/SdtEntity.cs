using System;
using ajiva.Ecs.Entity;
using ajiva.Ecs.Utils;
using ajiva.Utils;

namespace ajiva.Ecs.Example
{
    public class SdtEntity : AEntity, IUpdate
    {
        /// <inheritdoc />
        public void Update(UpdateInfo delta)
        {
            if (this.GetComponent<StdComponent>() is { } health)
            {
                health.Health += new Random().Next(-10, 10);
            }
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
        }
    }
}
