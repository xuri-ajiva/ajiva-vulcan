namespace Ajiva.Utils.Changing;

public interface IChangingObserver<TSender, TValue> where TSender : class where TValue : struct
{
    public delegate void OnChangedDelegate(TSender sender, TValue before, TValue after);
    TSender Owner { get; }
    Func<TValue> Result { get; }
    int ChangeThreshold { get; }
    int ChangedAmount { get; set; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue before, TValue after);
}
public interface IChangingObserver<TSender> where TSender : class
{
    public delegate void OnChangedDelegate(TSender sender);
    TSender Owner { get; }
    long Version { get; }
    event OnChangedDelegate OnChanged;
    void Changed();
}
public interface IChangingObserverOnlyAfter<TSender, TValue> where TSender : class where TValue : struct
{
    public delegate void OnChangedDelegate(TSender sender, TValue after);
    TSender Owner { get; }
    Func<TValue> Result { get; }
    int ChangeThreshold { get; }
    int ChangedAmount { get; set; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue after);
}
public interface IChangingObserverOnlyValue<TValue> where TValue : struct
{
    public delegate void OnChangedDelegate(TValue value);
    Func<TValue> Result { get; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue after);
}
public interface IChangingObserver
{
    int ChangeThreshold { get; }

    int ChangedAmount { get; set; }

    event OnChangedDelegate OnChanged;

    void Changed();
}
public interface IOverTimeChangingObserver
{
    public delegate void OnChangedDelegate(IOverTimeChangingObserver sender);

    public delegate void OnUpdateDelegate(IOverTimeChangingObserver sender);
    int DelayUpdateFor { get; }

    int ChangedAmount { get; set; }
    long ChangeBeginCycle { get; set; }

    bool Locked { get; }

    event OnChangedDelegate OnChanged;
    event OnUpdateDelegate OnUpdate;

    void Changed();
    void Updated();

    IDisposable BeginBigChange();

    void EndBigChange();
}
public delegate void OnUpdateDelegate(IChangingObserver sender);
public delegate void OnChangedDelegate(IChangingObserver sender);