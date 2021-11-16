namespace ajiva.Worker;

public delegate WorkResult Work(WorkInfo info, object? userParam);

public delegate void ErrorNotify(Exception exception);

public class WorkInfo
{
    internal WorkInfo(Work work, string name, ErrorNotify errorNotify, object? userParam = default)
    {
        Work = work;
        Name = name;
        ErrorNotify = errorNotify;
        UserParam = userParam;
    }

    public Worker? ActiveWorker { get; internal set; }
    public Work Work { get; }

    public ErrorNotify? ErrorNotify { get; }

    public string Name { get; }
    internal object? UserParam { get; }

    public WorkResult Invoke()
    {
        try
        {
            return Work.Invoke(this, UserParam);
        }
        catch (Exception e)
        {
            ErrorNotify?.Invoke(e);
            return WorkResult.Failed;
        }
    }
}