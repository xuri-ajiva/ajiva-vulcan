
namespace Ajiva.Ecs.Utils;

public interface IUpdate
{
    public void Update(UpdateInfo delta);
    
    public PeriodicUpdateInfo Info { get; }
}

