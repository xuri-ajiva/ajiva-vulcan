using ajiva.utils;

namespace ajiva.Ecs.Utils;

public interface IUpdate
{
    public void Update(UpdateInfo delta);
    
    public PeriodicUpdateInfo Info { get; }
}

