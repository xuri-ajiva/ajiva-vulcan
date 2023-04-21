using System.Numerics;

namespace Ajiva.Components.Transform;

public interface IModelMatTransform  : IComponent
{
    IChangingObserverOnlyValue<Matrix4x4> ChangingObserver { get; }
    Matrix4x4 ModelMat { get; }
}
