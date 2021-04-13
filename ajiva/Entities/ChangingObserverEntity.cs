using ajiva.Utils.Changing;

namespace ajiva.Entities
{
    public class ChangingObserverEntity : DefaultEntity
    {
        /// <inheritdoc />
        public ChangingObserverEntity(ChangingCacheMode mode)
        {
            Observer = new ChangingObserver(mode);
        }

        public IChangingObserver Observer { get; }
    }
}