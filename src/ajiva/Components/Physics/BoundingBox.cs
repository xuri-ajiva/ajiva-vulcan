using ajiva.Components.Transform;
using ajiva.Components.Transform.SpatialAcceleration;
using ajiva.Ecs;
using ajiva.Entities;
using ajiva.Models.Buffer;
using ajiva.Models.Vertex;
using ajiva.Utils.Changing;
using ajiva.Worker;
using GlmSharp;

namespace ajiva.Components.Physics;

public class BoundingBox : DisposingLogger, IBoundingBox
{
    private readonly IWorkerPool _workerPool;
    private StaticOctalItem<BoundingBox>? _octalItem;
    private readonly IModelMatTransform _transform;

    private uint _version;

    private DebugBox? _visual;

    public BoundingBox(IAjivaEcs ecs, IEntity entity, IWorkerPool workerPool)
    {
        _workerPool = workerPool;
        Ecs = ecs;
        Collider = entity.Get<CollisionsComponent>() as ICollider;
        _transform = entity.Get<Transform3d>();//entity.GetAny<IModelMatTransform>();
        Collider.ChangingObserver.OnChanged += ColliderChanged;
        _transform.ChangingObserver.OnChanged += TransformChanged;
    }

    /// <inheritdoc />
    public StaticOctalSpace Space => _octalItem?.Space ?? StaticOctalSpace.Empty;

    /// <inheritdoc />
    public ICollider Collider { get; }

    public IAjivaEcs Ecs { get; }

    private void TransformChanged(mat4 value)
    {
        ComputeBoxBackground();
    }

    public void ComputeBoxBackground()
    {
        lock (this)
        {
            var vCpy = ++_version;
            _workerPool.EnqueueWork((info, _) => vCpy < _version ? WorkResult.Failed : ComputeBox(), o => ALog.Error(o), nameof(ComputeBox));
        }
    }

    private StaticOctalTreeContainer<BoundingBox>? _octalTree;

    /// <inheritdoc />
    public void SetTree(StaticOctalTreeContainer<BoundingBox> octalTree)
    {
        this._octalTree = octalTree;
    }

    /// <inheritdoc />
    public void RemoveTree()
    {
        this._octalTree = null;
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
        var mm = _transform.ModelMat;
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
            if (_octalTree is not null)
            {
                var space = new StaticOctalSpace(new vec3(x1, y1, z1), new vec3(x2 - x1, y2 - y1, z2 - z1));
                if (_octalItem is not null)
                {
                    _octalItem = _octalTree.Relocate(_octalItem, space);
                }
                else
                {
                    _octalItem = _octalTree.Insert(this, space);
                }
            }

            UpdateDynamicDataVisual();

            return WorkResult.Succeeded;
        }
    }

    private void UpdateDynamicDataVisual()
    {
        return;
       /* if (_visual is null)
        {
            _visual = new DebugBox();
            _visual.Register(Ecs);
        }

        var scale = Space.Size / 2.0f;
        var position = Space.Position + scale;

        _visual.Configure<Transform3d>(trans =>
        {
            trans.RefPosition((ref vec3 vec) =>
            {
                vec.x = position[0];
                vec.y = position[1];
                vec.z = position[2];
            });

            trans.RefScale((ref vec3 vec) =>
            {
                vec.x = scale.x;
                vec.y = scale.y;
                vec.z = scale.z;
            });
        }); */
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        base.ReleaseUnmanagedResources(disposing);

        _transform.ChangingObserver.OnChanged += TransformChanged;
        Collider.ChangingObserver.OnChanged += ColliderChanged;
    }
}
