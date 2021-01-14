using ajiva.Engine;

namespace ajiva.Entitys
{
    public class AEntity : DisposingLogger
    {
        public AEntity(Transform3d transform, ARenewAble renewAble)
        {
            Transform = transform;
            RenewAble = renewAble;
        }

        public Transform3d Transform;

        public readonly ARenewAble RenewAble;

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Transform = Transform3d.Default;
            RenewAble.Dispose();
        }
    }
}
