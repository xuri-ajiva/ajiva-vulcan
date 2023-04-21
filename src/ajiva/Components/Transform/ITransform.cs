namespace Ajiva.Components.Transform;

public interface ITransform<TV, TM> : IDisposingLogger, IComponent where TV : struct where TM : struct
{
    public delegate void ModifyRef(ref TV vec);

    TV Position { get; set; }
    TV Rotation { get; set; }
    TV Scale { get; set; }
    TM ScaleMat { get; }
    TM RotationMat { get; }
    TM PositionMat { get; }
    TM ModelMat { get; }
    IChangingObserverOnlyValue<TM> ChangingObserver { get; }
    void RefPosition(ModifyRef mod);
    void RefRotation(ModifyRef mod);
    void RefScale(ModifyRef mod);
    string ToString();
}
