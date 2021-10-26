using System;
using System.Diagnostics;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Ecs.Component;
using ajiva.Ecs.Entity;
using ajiva.Entities;
using ajiva.Models;
using ajiva.Utils;
using ajiva.Utils.Changing;
using ajiva.Worker;
using Ajiva.Wrapper.Logger;
using GlmSharp;

namespace ajiva.Components.Physics
{
    public class BoundingBox : DisposingLogger, IComponent
    {
        public vec3 MinPos { get; private set; }
        public vec3 MaxPos { get; private set; }

        public IAjivaEcs Ecs { private get; set; }

        private ICollider? collider;
        public ICollider Collider
        {
            get => collider!;
            set
            {
                if (collider is not null)
                {
                    collider.ChangingObserver.OnChanged -= ColliderChangedDelegate;
                }
                value.ChangingObserver.OnChanged += ColliderChangedDelegate;
                collider = value;
            }
        }
        public Transform3d Transform
        {
            get => transform!;
            set
            {
                if (transform is not null)
                {
                    transform.ChangingObserver.OnChanged -= TransformChangedDelegate;
                }
                value.ChangingObserver.OnChanged += TransformChangedDelegate;

                transform = value;
            }
        }

        IChangingObserverOnlyAfter<ITransform<vec3, mat4>, mat4>.OnChangedDelegate TransformChangedDelegate;

        private void TransformChanged(ITransform<vec3, mat4> sender, mat4 after)
        {
            ComputeBoxBg();
        }

        private void ComputeBoxBg()
        {
            var vp = Ecs.GetSystem<WorkerPool>();
            lock (this)
            {
                var vCpy = ++version;
                vp.EnqueueWork((info, _) => vCpy < version ? WorkResult.Failed : ComputeBox(), ALog.Error, nameof(ComputeBox));
            }
        }

        private OnChangedDelegate ColliderChangedDelegate;

        private void ColliderChanged(IChangingObserver changingObserver)
        {
            ComputeBoxBg();
        }

        private DebugBox? visual;
        private Transform3d? transform;
        private uint version = 0;

        public BoundingBox()
        {
            ColliderChangedDelegate = ColliderChanged;
            TransformChangedDelegate = TransformChanged;
        }

        private WorkResult ComputeBox()
        {
            var mesh = collider!.Pool.GetMesh(collider.MeshId);
            if (mesh is not Mesh<Vertex3D> vMesh) return WorkResult.Failed;

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
                MinPos = new vec3(x1, y1, z1);
                MaxPos = new vec3(x2, y2, z2);

                if (visual is null)
                {
                    Debug.Assert(Ecs.TryCreateEntity<DebugBox>(out visual), "Ecs.TryCreateEntity<BoxRenderer>(out visual)");
                }

                if (visual.TryGetComponent<Transform3d>(out var trans))
                {
                    var size = (MaxPos - MinPos) / 2;
                    trans.Position = MinPos + size;
                    trans.Scale = size * 1.01f;
                }
                return WorkResult.Succeeded;
            }
        }
    }
    internal class BoxRenderer : DefaultEntity
    {
    }
}
