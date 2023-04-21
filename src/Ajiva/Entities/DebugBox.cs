using Ajiva.Components.Mesh;
using Ajiva.Components.Transform;
using Ajiva.Ecs.Entity.Helper;
using Ajiva.Systems.VulcanEngine.Debug;

namespace Ajiva.Entities;

[EntityComponent(typeof(Transform3d), typeof(DebugComponent))]
public partial class DebugBox
{
    protected void InitializeDefault()
    {
        Transform3d ??= new Transform3d();
        DebugComponent ??= new DebugComponent(MeshPrefab.Cube, Transform3d) {
            DrawTransform = true,
            DrawWireframe = true,
            Render = true
        };
    }
}
