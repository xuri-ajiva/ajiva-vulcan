using System;
using ajiva.Components.Media;
using ajiva.Utils;

namespace ajiva.Entities
{
    public class TransFormEntity : DefaultEntity
    {
        public TransFormEntity()
        {
            TransformLazy = new(GetComponent<Transform3d>);
        }

        public Lazy<Transform3d> TransformLazy { get; }
        public Transform3d Transform => TransformLazy.Value;

        /// <inheritdoc />
        public override void Update(UpdateInfo delta)
        {
        }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
        }
    }
}
