using System.Numerics;
using ajiva.Utils.Changing;

namespace ajiva.Components.Transform;

public interface IModelMatTransform  : IComponent
{
    IChangingObserverOnlyValue<Matrix4x4> ChangingObserver { get; }
    Matrix4x4 ModelMat { get; }
}
