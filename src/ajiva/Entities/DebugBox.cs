using ajiva.Components.Mesh;
using ajiva.Components.Transform;
using ajiva.Ecs.Entity.Helper;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Entities;

[EntityComponent(typeof(Transform3d), typeof(DebugComponent))]
public partial class DebugBox
{
    public DebugBox()
    {
        Transform3d = new Transform3d();
        DebugComponent = new DebugComponent(MeshPrefab.Cube, Transform3d) {
            DrawTransform = true,
            DrawWireframe = true,
            Render = true
        };
    }
}
