namespace ajiva.Models;

public delegate void ActionRef<TIn>(ref TIn value);

public delegate TOut FuncRef<TIn, out TOut>(ref TIn value);
