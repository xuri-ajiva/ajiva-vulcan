﻿using System.Numerics;
using Ajiva.Components.Mesh;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform;
using Ajiva.Components.Transform.SpatialAcceleration;
using Ajiva.Entities;
using Ajiva.Models.Buffer;
using Ajiva.Models.Vertex;
using Ajiva.Worker;

namespace Ajiva.Components.Physics;

public class BoundingBox : DisposingLogger, IBoundingBox
{
    private readonly IEntity _entity;
    private readonly IModelMatTransform _transform;
    private readonly IWorkerPool _workerPool;

    private readonly Lazy<Mesh<Vertex3D>> meshLazy;
    private StaticOctalItem<BoundingBox>? _octalItem;

    private StaticOctalTreeContainer<BoundingBox>? _octalTree;

    private uint _version;

    private DebugBox? _visual;

    public BoundingBox(IEntity entity, IWorkerPool workerPool)
    {
        _entity = entity;
        _workerPool = workerPool;
        meshLazy = new Lazy<Mesh<Vertex3D>>(() => (Mesh<Vertex3D>)_entity.Get<RenderInstanceMesh>().Mesh);
        _transform = entity.Get<Transform3d>(); //entity.GetAny<IModelMatTransform>();
        _transform.ChangingObserver.OnChanged += TransformChanged;
    }

    /// <inheritdoc />
    public StaticOctalSpace Space => _octalItem?.Space ?? StaticOctalSpace.Empty;

    public void ComputeBoxBackground()
    {
        lock (this)
        {
            var vCpy = ++_version;
            _workerPool.EnqueueWork((info, _) => vCpy < _version ? WorkResult.Failed : ComputeBox(), o => Log.Error(o, o.Message), nameof(ComputeBox));
        }
    }

    /// <inheritdoc />
    public void SetTree(StaticOctalTreeContainer<BoundingBox> octalTree)
    {
        _octalTree = octalTree;
    }

    /// <inheritdoc />
    public void RemoveTree()
    {
        _octalTree = null;
    }

    private void TransformChanged(Matrix4x4 value)
    {
        ComputeBoxBackground();
    }

    private void ColliderChanged(IChangingObserver changingObserver)
    {
        ComputeBoxBackground();
    }

    private WorkResult ComputeBox()
    {
        if (meshLazy.Value.VertexBuffer is not BufferOfT<Vertex3D> buff)
            return WorkResult.Failed;

        float x1 = float.PositiveInfinity, x2 = float.NegativeInfinity, y1 = float.PositiveInfinity, y2 = float.NegativeInfinity, z1 = float.PositiveInfinity, z2 = float.NegativeInfinity; // 1 = min, 2 = max
        var mm = _transform.ModelMat;
        for (var i = 0; i < buff.Length; i++)
        {
            var v = Vector3.Transform(buff[i].Position, mm);
            if (x1 > v.X)
                x1 = v.X;
            if (x2 < v.X)
                x2 = v.X;

            if (y1 > v.Y)
                y1 = v.Y;
            if (y2 < v.Y)
                y2 = v.Y;

            if (z1 > v.Z)
                z1 = v.Z;
            if (z2 < v.Z)
                z2 = v.Z;
        }

        lock (this)
        {
            if (_octalTree is not null)
            {
                var space = new StaticOctalSpace(new Vector3(x1, y1, z1), new Vector3(x2 - x1, y2 - y1, z2 - z1));
                if (_octalItem is not null)
                    _octalItem = _octalTree.Relocate(_octalItem, space);
                else
                    _octalItem = _octalTree.Insert(this, space);
            }

            UpdateDynamicDataVisual();

            return WorkResult.Succeeded;
        }
    }

    private void UpdateDynamicDataVisual()
    {
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
    }
}