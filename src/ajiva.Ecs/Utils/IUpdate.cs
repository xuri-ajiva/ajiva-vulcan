using System.Threading;

namespace ajiva.Ecs.Utils;

public interface IUpdate
{
    public void Update(UpdateInfo delta);
    
    public PeriodicTimer Timer { get; }
}
