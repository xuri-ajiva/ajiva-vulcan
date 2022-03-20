using ajiva.Components.Transform;
using ajiva.Components.Transform.Ui;

namespace ajiva.Systems.VulcanEngine.Interfaces;

public interface ITransformComponentSystem : IComponentSystem<Transform3d>
{
}

public interface ITransform2dComponentSystem: IComponentSystem<UiTransform>
{
}
