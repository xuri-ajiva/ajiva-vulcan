using ajiva.Utils;

namespace ajiva.Ecs.System
{
    public abstract class SystemBase : DisposingLogger, ISystem
    {
        protected AjivaEcs Ecs { get; private set; }

        protected virtual void Setup() { }

        /// <inheritdoc />
        public void Setup(AjivaEcs ecs)
        {
            Ecs = ecs;
            Setup();
        }
    }
}
