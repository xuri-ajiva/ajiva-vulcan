using Ajiva.Components.Transform;
using Ajiva.Components.Transform.Ui;

namespace Ajiva.Systems.VulcanEngine.Interfaces;

public interface ITransformComponentSystem : IComponentSystem<Transform3d>
{
}
public interface ITransform2dComponentSystem : IComponentSystem<UiTransform>
{
}