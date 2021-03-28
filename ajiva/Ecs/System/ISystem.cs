using ajiva.Utils;

namespace ajiva.Ecs.System
{
    public interface ISystem : IDisposingLogger
    {
        void Setup(AjivaEcs ecs);
    }
}
