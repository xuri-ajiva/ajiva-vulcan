using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.Transform;

public interface IModelMatTransform  : IComponent
{
    IChangingObserverOnlyValue<mat4> ChangingObserver { get; }
    mat4 ModelMat { get; }
}