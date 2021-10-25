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
            ComputeBox();
        }

        private OnChangedDelegate ColliderChangedDelegate;

        private void ColliderChanged(IChangingObserver changingObserver)
        {
            ComputeBox();
        }

        private DebugBox? visual;
        private Transform3d? transform;

        public BoundingBox()
        {
            ColliderChangedDelegate = ColliderChanged;
            TransformChangedDelegate = TransformChanged;
        }

        private void ComputeBox()
        {
            var mesh = collider!.Pool.GetMesh(collider.MeshId);
            if (mesh is not Mesh<Vertex3D> vMesh) return;

            float x1 = 0, x2 = 0, y1 = 0, y2 = 0, z1 = 0, z2 = 0; // 1 = min, 2 = max
            for (var i = 0; i < vMesh.Vertices.Length; i++)
            {
                var v = vMesh.Vertices[i].Position;
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

            MinPos = new vec3(Transform.ModelMat * new vec4(x1, y1, z1, 1));
            MaxPos = new vec3(Transform.ModelMat * new vec4(x2, y2, z2, 1));

            if (visual is null)
            {
                Debug.Assert(Ecs.TryCreateEntity<DebugBox>(out visual), "Ecs.TryCreateEntity<BoxRenderer>(out visual)");
            }
            if (visual.TryGetComponent<Transform3d>(out var trans))
            {
                trans.Position = MinPos + ((MaxPos - MinPos) / 2);
                trans.Scale = Transform.Scale * 1.1f;
                trans.Rotation = Transform.Rotation;
            }
        }
    }
    internal class BoxRenderer : DefaultEntity
    {
    }
}
