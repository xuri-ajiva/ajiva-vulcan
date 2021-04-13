using ajiva.Utils;

namespace ajiva.Ecs.System
{
    public abstract class SystemBase : DisposingLogger, ISystem
    {
        protected AjivaEcs Ecs { get; }

        /// <inheritdoc />
        protected SystemBase(AjivaEcs ecs)
        {
            Ecs = ecs;
        }

        /// <inheritdoc />
        public virtual void Setup()
        {
            
        }
    }
}
