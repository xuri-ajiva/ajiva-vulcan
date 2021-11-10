using ajiva.Utils.Changing;

namespace ajiva.Components.Transform;

public interface ITransform<TV, TM> : IDisposingLogger, IComponent where TV : struct, IReadOnlyList<float> where TM : struct, IReadOnlyList<float>
{
    TV Position { get; set; }
    TV Rotation { get; set; }
    TV Scale { get; set; }
    TM ScaleMat { get; }
    TM RotationMat { get; }
    TM PositionMat { get; }
    TM ModelMat { get; }
    IChangingObserverOnlyAfter<ITransform<TV, TM>, TM> ChangingObserver { get; }
    void RefPosition(ModifyRef mod);
    void RefRotation(ModifyRef mod);
    void RefScale(ModifyRef mod);
    string ToString();

    public delegate void ModifyRef(ref TV vec);
}