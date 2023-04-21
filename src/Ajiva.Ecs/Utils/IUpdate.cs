namespace Ajiva.Ecs.Utils;

public interface IUpdate
{
    public PeriodicUpdateInfo Info { get; }
    public void Update(UpdateInfo delta);
}