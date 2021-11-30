using ajiva.Components.Transform;
using ajiva.Components.Transform.Kd;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Models;
using ajiva.Models.Buffer;
using ajiva.Utils.Changing;
using ajiva.Worker;
using GlmSharp;

namespace ajiva.Components.Physics;

public class BoundingBox : DisposingLogger, IBoundingBox
{
    private readonly IModelMatTransform transform;

    private uint version;

    private DebugBox? visual;

    public BoundingBox(IAjivaEcs ecs, IEntity entity)
    {
        Ecs = ecs;
        Collider = entity.Get<ICollider>();
        transform = entity.GetAny<IModelMatTransform>();
        Collider.ChangingObserver.OnChanged += ColliderChanged;
        transform.ChangingObserver.OnChanged += TransformChanged;
        MinPos = new KdVec(3);
        MaxPos = new KdVec(3);
        Center = new KdTransform(3);
    }

    /// <inheritdoc />
    public KdVec MaxPos { get; }

    /// <inheritdoc />
    public ICollider Collider { get; }

    /// <inheritdoc />
    public KdTransform Center { get; }

    /// <inheritdoc />
    public KdVec MinPos { get; }

    public IAjivaEcs Ecs { get; }

    private void TransformChanged(mat4 value)
    {
        ComputeBoxBackground();
    }

    public void ComputeBoxBackground()
    {
        var vp = Ecs.Get<WorkerPool>();
        lock (this)
        {
            var vCpy = ++version;
            vp.EnqueueWork((info, _) => vCpy < version ? WorkResult.Failed : ComputeBox(), ALog.Error, nameof(ComputeBox));
        }
    }

    private void ColliderChanged(IChangingObserver changingObserver)
    {
        ComputeBoxBackground();
    }

    private WorkResult ComputeBox()
    {
        var mesh = Collider!.Pool.GetMesh(Collider.MeshId);
        if (mesh.VertexBuffer is null) return WorkResult.Failed;
        var buff = (BufferOfT<Vertex3D>)mesh.VertexBuffer;
        
        float x1 = float.PositiveInfinity, x2 = float.NegativeInfinity, y1 = float.PositiveInfinity, y2 = float.NegativeInfinity, z1 = float.PositiveInfinity, z2 = float.NegativeInfinity; // 1 = min, 2 = max
        var mm = transform.ModelMat;
        for (var i = 0; i < buff.Length; i++)
        {
            var v = mm * buff[i].Position;
            if (x1 > v.x)
                x1 = v.x;
            if (x2 < v.x)
                x2 = v.x;

            if (y1 > v.y)
                y1 = v.y;
            if (y2 < v.y)
                y2 = v.y;

            if (z1 > v.z)
                z1 = v.z;
            if (z2 < v.z)
                z2 = v.z;
        }

        lock (this)
        {
            MinPos.Update(x1, y1, z1);
            MaxPos.Update(x2, y2, z2);

            UpdateDynamicDataVisual();

            return WorkResult.Succeeded;
        }
    }

    private void UpdateDynamicDataVisual()
    {
        if (visual is null)
        {
            visual = new DebugBox();
            visual.Register(Ecs);
        }

        var size = (MaxPos - MinPos) / 2.0f;

        Center.Scale.Update(size);
        Center.Position.Update(MinPos + size);
        
        visual.Configure<Transform3d>(trans =>
        {
            trans.RefPosition((ref vec3 vec) =>
            {
                vec.x = Center.Position[0];
                vec.y = Center.Position[1];
                vec.z = Center.Position[2];
            });

            trans.RefScale((ref vec3 vec) =>
            {
                vec.x = size[0];
                vec.y = size[1];
                vec.z = size[2];
            });
        });

    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);

        transform.ChangingObserver.OnChanged += TransformChanged;
        Collider.ChangingObserver.OnChanged += ColliderChanged;
    }
}
