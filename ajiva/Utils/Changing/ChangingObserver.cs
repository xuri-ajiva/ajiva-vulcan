namespace ajiva.Utils.Changing
{
    public class ChangingObserver : IChangingObserver
    {
        public ChangingObserver(int delayUpdateFor)
        {
            DelayUpdateFor = delayUpdateFor;
        }

        /// <inheritdoc />
        public int DelayUpdateFor { get; }

        /// <inheritdoc />
        public int ChangedAmount { get; set; } = 0;

        /// <inheritdoc />
        public long ChangeBeginCycle { get; set; } = 0;

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
                ChangeBeginCycle = 0;
            }
            OnUpdate?.Invoke(this);
        }
    }
}
