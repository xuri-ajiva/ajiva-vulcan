using ajiva.Ecs.Component;
using ajiva.Models;
using ajiva.Systems;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Components
{
    public class ChangingComponentBase : DisposingLogger, IComponent
    {
        /// <inheritdoc />
        public ChangingComponentBase(ChangingCacheMode mode)
        {
            ChangingObserver = new ChangingObserver(mode);
        }

        public IChangingObserver ChangingObserver { get; }
    }
}
