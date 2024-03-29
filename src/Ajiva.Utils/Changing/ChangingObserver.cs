﻿namespace Ajiva.Utils.Changing;

public class ChangingObserver : IChangingObserver
{
    public ChangingObserver(int changeThreshold)
    {
        ChangeThreshold = changeThreshold;
    }

    private object Lock { get; } = new object();

    /// <inheritdoc />
    public int ChangeThreshold { get; }

    /// <inheritdoc />
    public int ChangedAmount { get; set; }

    /// <inheritdoc />
    public event OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public void Changed()
    {
        lock (Lock)
        {
            ChangedAmount++;
            if (ChangedAmount > ChangeThreshold)
            {
                ChangedAmount = 0;

                OnChanged?.Invoke(this);
            }
        }
    }
}
public class ChangingObserverOnlyAfter<TSender, TValue> : IChangingObserverOnlyAfter<TSender, TValue> where TSender : class where TValue : struct
{
    public ChangingObserverOnlyAfter(TSender owner, Func<TValue> result, int changeThreshold)
    {
        Owner = owner;
        Result = result;
        ChangeThreshold = changeThreshold;
    }

    private object Lock { get; } = new object();

    public TSender Owner { get; }

    /// <inheritdoc />
    public Func<TValue> Result { get; }

    /// <inheritdoc />
    public int ChangeThreshold { get; }

    /// <inheritdoc />
    public int ChangedAmount { get; set; }

    /// <inheritdoc />
    public event IChangingObserverOnlyAfter<TSender, TValue>.OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public void Changed(TValue after)
    {
        lock (Lock)
        {
            ChangedAmount++;
            if (ChangedAmount > ChangeThreshold)
            {
                ChangedAmount = 0;
                OnChanged?.Invoke(Owner, after);
            }
        }
    }
}
public class ChangingObserver<TSender, TValue> : IChangingObserver<TSender, TValue> where TSender : class where TValue : struct
{
    public ChangingObserver(TSender owner, Func<TValue> result, int changeThreshold)
    {
        Owner = owner;
        Result = result;
        ChangeThreshold = changeThreshold;
    }

    private object Lock { get; } = new object();

    public TSender Owner { get; }

    /// <inheritdoc />
    public Func<TValue> Result { get; }

    /// <inheritdoc />
    public int ChangeThreshold { get; }

    /// <inheritdoc />
    public int ChangedAmount { get; set; }

    /// <inheritdoc />
    public event IChangingObserver<TSender, TValue>.OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public void Changed(TValue before, TValue after)
    {
        lock (Lock)
        {
            ChangedAmount++;
            if (ChangedAmount > ChangeThreshold)
            {
                ChangedAmount = 0;

                OnChanged?.Invoke(Owner, before, after);
            }
        }
    }
}
public class ChangingObserverOnlyValue<TValue> : IChangingObserverOnlyValue<TValue> where TValue : struct
{
    public ChangingObserverOnlyValue(Func<TValue> result)
    {
        Result = result;
    }

    /// <inheritdoc />
    public Func<TValue> Result { get; set; }

    /// <inheritdoc />
    public event IChangingObserverOnlyValue<TValue>.OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public void Changed(TValue value)
    {
        lock (this)
        {
            OnChanged?.Invoke(value);
        }
    }
}
public class ChangingObserver<TSender> : IChangingObserver<TSender> where TSender : class
{
    public ChangingObserver(TSender owner)
    {
        Owner = owner;
    }

    private object Lock { get; } = new object();

    /// <inheritdoc />
    public TSender Owner { get; set; }

    /// <inheritdoc />
    public event IChangingObserver<TSender>.OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public long Version { get; private set; }

    /// <inheritdoc />
    public void Changed()
    {
        lock (Lock)
        {
            Version++;
            OnChanged?.Invoke(Owner);
        }
    }
}
public class OverTimeChangingObserver : IOverTimeChangingObserver
{
    public OverTimeChangingObserver(int delayUpdateFor)
    {
        DelayUpdateFor = delayUpdateFor;
    }

    private object Lock { get; } = new object();

    public int BigChangeCount { get; set; }
    public BachChange? Current { get; set; }

    /// <inheritdoc />
    public int DelayUpdateFor { get; }

    /// <inheritdoc />
    public int ChangedAmount { get; set; }

    /// <inheritdoc />
    public long ChangeBeginCycle { get; set; }

    /// <inheritdoc />
    public event IOverTimeChangingObserver.OnChangedDelegate? OnChanged;

    /// <inheritdoc />
    public event IOverTimeChangingObserver.OnUpdateDelegate? OnUpdate;

    /// <inheritdoc />
    public void Changed()
    {
        lock (Lock)
        {
            ChangedAmount++;
        }
        OnChanged?.Invoke(this);
    }

    /// <inheritdoc />
    public void Updated()
    {
        lock (Lock)
        {
            ChangedAmount = 0;
            ChangeBeginCycle = 0;
        }
        OnUpdate?.Invoke(this);
    }

    public IDisposable BeginBigChange()
    {
        BigChangeCount++;
        Current ??= new BachChange(this);
        return Current;
    }

    public void EndBigChange()
    {
        BigChangeCount--;
        if (BigChangeCount <= 0)
        {
            BigChangeCount = 0;
            if (Current is not null)
            {
                if (ChangedAmount > Current.BeginChange) ChangeBeginCycle = Current.BeginCycle; // - DelayUpdateFor ?
                Current = null;
            }
        }
    }

    /// <inheritdoc />
    public bool Locked => Current is not null;

    public class BachChange : DisposingLogger
    {
        /// <inheritdoc />
        public BachChange(IOverTimeChangingObserver parent)
        {
            Parent = parent;
            BeginCycle = parent.ChangeBeginCycle;
            BeginChange = parent.ChangedAmount;
        }

        public long BeginCycle { get; set; }
        public int BeginChange { get; }
        private IOverTimeChangingObserver Parent { get; }

        /// <inheritdoc />
        protected override void ReleaseUnmanagedResources(bool disposing)
        {
            base.ReleaseUnmanagedResources(disposing);
            Parent.EndBigChange();
        }
    }
}