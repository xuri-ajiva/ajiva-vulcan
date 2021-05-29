using ajiva.Ecs.Component;
using ajiva.Utils;
using ajiva.Utils.Changing;

namespace ajiva.Components
{
    public class ChangingComponentBase : DisposingLogger, IComponent
    {
        /// <inheritdoc />
        public ChangingComponentBase(int delayUpdateFor)
        {
            ChangingObserver = new ChangingObserver(delayUpdateFor);
        }

        public IChangingObserver ChangingObserver { get; }
    }
}
