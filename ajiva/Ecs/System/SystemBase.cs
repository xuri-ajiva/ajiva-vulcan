using ajiva.Helpers;

namespace ajiva.Ecs.System
{
    public abstract class SystemBase : DisposingLogger, ISystem
    {
        protected AjivaEcs Ecs { get; private set; }

        protected abstract void Setup();

        /// <inheritdoc />
        public void Setup(AjivaEcs ecs)
        {
            Ecs = ecs;
            Setup();
        }
    }
}
