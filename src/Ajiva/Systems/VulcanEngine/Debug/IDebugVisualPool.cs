using Ajiva.Components.Transform.SpatialAcceleration;

namespace Ajiva.Systems.VulcanEngine.Debug;

public interface IDebugVisualPool
{
    void UpdateVisual(object owner, StaticOctalSpace area);
    void DestroyVisual(object owner);
    void CreateVisual(object owner, StaticOctalSpace area);
}
