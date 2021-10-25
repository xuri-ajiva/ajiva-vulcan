using System;
using ajiva.Components.RenderAble;
using ajiva.Ecs.Component;
using ajiva.Models;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Components.Physics
{
    public class CollisionsComponent : ChangingComponentBase, ICollider
    {
        private uint meshId;

        /// <inheritdoc />
        public CollisionsComponent() : base(0)
        {
        }

        /// <inheritdoc />
        public uint MeshId
        {
            get => meshId;
            set => ChangingObserver.RaiseAndSetIfChanged(ref meshId, value);
        }

        /// <inheritdoc />
        public MeshPool Pool { get; set; }
    }
}
