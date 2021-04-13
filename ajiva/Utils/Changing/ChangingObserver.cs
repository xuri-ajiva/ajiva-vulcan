namespace ajiva.Utils.Changing
{
    public class ChangingObserver : IChangingObserver
    {
        public ChangingObserver(ChangingCacheMode mode)
        {
            Mode = mode;
        }

        public ChangingObserver() : this(ChangingCacheMode.NextCycleUpdate)
        {
        }

        /// <inheritdoc />
        public ChangingCacheMode Mode { get; set; }

        /// <inheritdoc />
        public int ChangedAmount { get; set; } = 0;

        /// <inheritdoc />
        public ulong ChangeBeginCycle { get; set; } = 0;

        private object _lock { get; } = new();

        /// <inheritdoc />
        public event OnChangedDelegate? OnChanged;

        /// <inheritdoc />
        public event OnUpdateDelegate? OnUpdate;

        /// <inheritdoc />
        public void Changed()
        {
            lock (_lock)
            {
                ChangedAmount++;
            }
            OnChanged?.Invoke(this);
        }

        /// <inheritdoc />
        public void Updated()
        {
            lock (_lock)
            {
                ChangedAmount = 0;
            }
            OnUpdate?.Invoke(this);
        }
    }
}