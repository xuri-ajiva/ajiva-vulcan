namespace Ajiva.Utils.Changing;

public interface IChangingObserver<TSender, TValue> where TSender : class where TValue : struct
{
    TSender Owner { get; }
    Func<TValue> Result { get; }
    int ChangeThreshold { get; }
    int ChangedAmount { get; set; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue before, TValue after);

    public delegate void OnChangedDelegate(TSender sender, TValue before, TValue after);
}
public interface IChangingObserver<TSender> where TSender : class
{
    TSender Owner { get; }
    event OnChangedDelegate OnChanged;
    long Version { get; }
    void Changed();

    public delegate void OnChangedDelegate(TSender sender);
}
public interface IChangingObserverOnlyAfter<TSender, TValue> where TSender : class where TValue : struct
{
    TSender Owner { get; }
    Func<TValue> Result { get; }
    int ChangeThreshold { get; }
    int ChangedAmount { get; set; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue after);

    public delegate void OnChangedDelegate(TSender sender, TValue after);
}
public interface IChangingObserverOnlyValue<TValue> where TValue : struct
{
    Func<TValue> Result { get; }
    event OnChangedDelegate OnChanged;
    void Changed(TValue after);
    public delegate void OnChangedDelegate(TValue value);
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
    int DelayUpdateFor { get; }

    int ChangedAmount { get; set; }
    long ChangeBeginCycle { get; set; }

    event OnChangedDelegate OnChanged;
    event OnUpdateDelegate OnUpdate;

    void Changed();
    void Updated();

    public delegate void OnUpdateDelegate(IOverTimeChangingObserver sender);

    public delegate void OnChangedDelegate(IOverTimeChangingObserver sender);

    IDisposable BeginBigChange();

    void EndBigChange();

    bool Locked { get; }
}

public delegate void OnUpdateDelegate(IChangingObserver sender);

public delegate void OnChangedDelegate(IChangingObserver sender);