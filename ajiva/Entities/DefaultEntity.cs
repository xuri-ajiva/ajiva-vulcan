﻿using ajiva.Ecs.Entity;
using ajiva.Utils;

namespace ajiva.Entities
{
    public class DefaultEntity : AEntity
    {
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }

        /// <inheritdoc />
        public override void Update(UpdateInfo delta)
        {
        }
    }
}
