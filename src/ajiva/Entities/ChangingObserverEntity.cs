using ajiva.Utils.Changing;

namespace ajiva.Entities
{
    public class ChangingObserverEntity : DefaultEntity
    {
        /// <inheritdoc />
        public ChangingObserverEntity(int delayUpdateFor)
        {
            Observer = new ChangingObserver(delayUpdateFor);
        }

        public IChangingObserver Observer { get; }
    }
}
