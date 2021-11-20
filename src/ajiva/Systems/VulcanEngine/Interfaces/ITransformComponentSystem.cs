using ajiva.Components.Transform;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface ITransformComponentSystem : IComponentSystem<Transform3d>
{
}

public interface ITransform2dComponentSystem: IComponentSystem<Transform2d>
{
}
