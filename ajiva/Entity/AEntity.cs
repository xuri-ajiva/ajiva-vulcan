using ajiva.Engine;

namespace ajiva.Entity
{
    public class AEntity : DisposingLogger
    {
        public AEntity(Transform3d transform, ARenderAble renderAble)
        {
            Transform = transform;
            RenderAble = renderAble;
        }

        public Transform3d Transform { get; private set; }

        public  ARenderAble RenderAble { get; private set; }
        
        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources()
        {
            Transform = Transform3d.Default;
            RenderAble.Dispose();
        }
    }
}
