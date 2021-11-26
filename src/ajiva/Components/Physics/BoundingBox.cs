using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Models;
using ajiva.Utils.Changing;
using ajiva.Worker;
using GlmSharp;

namespace ajiva.Components.Physics;

public class BoundingBox : DisposingLogger, IComponent
{
    private ICollider? collider;

    private readonly OnChangedDelegate ColliderChangedDelegate;
    private vec3 maxPos;
    private vec3 minPos;
    private Transform3d? transform;

    private readonly IChangingObserverOnlyAfter<ITransform<vec3, mat4>, mat4>.OnChangedDelegate TransformChangedDelegate;
    private uint version;

    private DebugBox? visual;

    public BoundingBox()
    {
        ColliderChangedDelegate = ColliderChanged;
        TransformChangedDelegate = TransformChanged;
    }

    public vec3 MinPos
    {
        get => minPos;
        private set
        {
            minPos = value;
            CalculateDynamics();
        }
    }
    public vec3 MaxPos
    {
        get => maxPos;
        private set
        {
            maxPos = value;
            CalculateDynamics();
        }
    }
    public vec3 SizeHalf { get; private set; }
    public vec3 Center { get; private set; }

    public IAjivaEcs Ecs { private get; set; }
    public ICollider Collider
    {
        get => collider!;
        set
        {
            if (collider is not null) collider.ChangingObserver.OnChanged -= ColliderChangedDelegate;
            value.ChangingObserver.OnChanged += ColliderChangedDelegate;
            collider = value;
        }
    }
    public Transform3d Transform
    {
        get => transform!;
        set
        {
            if (transform is not null) transform.ChangingObserver.OnChanged -= TransformChangedDelegate;
            value.ChangingObserver.OnChanged += TransformChangedDelegate;

            transform = value;
        }
    }

    private void CalculateDynamics()
    {
        SizeHalf = (MaxPos - MinPos) / 2;
        Center = MinPos + SizeHalf;
    }

    private void TransformChanged(ITransform<vec3, mat4> sender, mat4 after)
    {
        ComputeBoxBg();
    }

    public void ComputeBoxBg()
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
        ComputeBoxBg();
    }

    private WorkResult ComputeBox()
    {
        var mesh = collider!.Pool.GetMesh(collider.MeshId);
        if (mesh is not Mesh<Vertex3D> vMesh) return WorkResult.Failed;
        if (vMesh.Vertices is null) return WorkResult.Failed;

        float x1 = float.PositiveInfinity, x2 = float.NegativeInfinity, y1 = float.PositiveInfinity, y2 = float.NegativeInfinity, z1 = float.PositiveInfinity, z2 = float.NegativeInfinity; // 1 = min, 2 = max
        var mm = Transform.ModelMat;
        for (var i = 0; i < vMesh.Vertices.Length; i++)
        {
            var v = mm * vMesh.Vertices[i].Position;
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
            minPos = new vec3(x1, y1, z1); //no update
            MaxPos = new vec3(x2, y2, z2); //update dynamics

            UpdateVisual();

            return WorkResult.Succeeded;
        }
    }

    private void UpdateVisual()
    {
        if (visual is null)
        {
            visual = new DebugBox();
            visual.Register(Ecs);
        }

        visual.Configure<Transform3d>(trans =>
        {
            trans.Position = Center;
            trans.Scale = SizeHalf * 1.01f;
        });
    }

    public void ModifyPositionRelative(vec3 vec3)
    {
        minPos += vec3;
        MaxPos += vec3;
        UpdateVisual();
    }
}
