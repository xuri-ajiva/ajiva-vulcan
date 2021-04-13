namespace ajiva.Utils.Changing
{
    public interface IChangingObserver
    {
        int DelayUpdateFor { get; }

        int ChangedAmount { get; set; }
        long ChangeBeginCycle { get; set; }

        event OnChangedDelegate OnChanged;
        event OnUpdateDelegate OnUpdate;

        void Changed();
        void Updated();
    }

    public delegate void OnUpdateDelegate(IChangingObserver sender);

    public delegate void OnChangedDelegate(IChangingObserver sender);
}
