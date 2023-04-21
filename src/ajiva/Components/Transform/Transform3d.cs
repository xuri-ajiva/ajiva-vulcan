using System.Numerics;

namespace Ajiva.Components.Transform;

public class Transform3d : DisposingLogger, ITransform<Vector3, Matrix4x4>, IModelMatTransform
{
    private Vector3 position;
    private Vector3 rotation;
    private Vector3 scale;

    public Transform3d(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        ChangingObserver = new ChangingObserverOnlyValue<Matrix4x4>(() => ModelMat);
        ChangingObserver.RaiseAndSetIfChanged(ref this.position, position);
        ChangingObserver.RaiseAndSetIfChanged(ref this.rotation, rotation);
        ChangingObserver.RaiseAndSetIfChanged(ref this.scale, scale);
    }
    public void UpdateAll()
    {
        ChangingObserver.Changed(ChangingObserver.Result());
    }
    public Transform3d(Vector3 position, Vector3 rotation) : this(position, rotation, Vector3.One)
    {
    }

    public Transform3d(Vector3 position) : this(position, Vector3.Zero)
    {
    }

    public Transform3d() : this(Vector3.Zero)
    {
    }

    public Matrix4x4 ScaleMat => Matrix4x4.CreateScale(Scale);
    public Matrix4x4 RotationMat => Matrix4x4.CreateRotationX(MathX.Radians(Rotation.X)) * Matrix4x4.CreateRotationY(MathX.Radians(Rotation.Y)) * Matrix4x4.CreateRotationZ(MathX.Radians(Rotation.Z));
    public Matrix4x4 PositionMat => Matrix4x4.CreateTranslation(Position);

    public Matrix4x4 ModelMat => PositionMat * RotationMat * ScaleMat;

    public override string ToString()
    {
        return $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}, {nameof(Scale)}: {Scale}";
    }

#region propatys

    public Vector3 Position
    {
        get => position;
        set => ChangingObserver.RaiseAndSetIfChanged(ref position, value);
    }
    public Vector3 Rotation
    {
        get => rotation;
        set => ChangingObserver.RaiseAndSetIfChanged(ref rotation, value);
    }
    public Vector3 Scale
    {
        get => scale;
        set => ChangingObserver.RaiseAndSetIfChanged(ref scale, value);
    }

    /// <inheritdoc />
    public IChangingObserverOnlyValue<Matrix4x4> ChangingObserver { get; set; }

    public void RefPosition(ITransform<Vector3, Matrix4x4>.ModifyRef mod)
    {
        var value = position;
        mod?.Invoke(ref position);
        ChangingObserver.RaiseIfChanged(position, value);
    }

    public void RefRotation(ITransform<Vector3, Matrix4x4>.ModifyRef mod)
    {
        var value = rotation;
        mod?.Invoke(ref rotation);
        ChangingObserver.RaiseIfChanged(rotation, value);
    }

    public void RefScale(ITransform<Vector3, Matrix4x4>.ModifyRef mod)
    {
        var value = scale;
        mod?.Invoke(ref scale);
        ChangingObserver.RaiseIfChanged(scale, value);
    }

#endregion
}
