using ajiva.Components.Mesh;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Entities;

public class DebugBox : DefaultEntity
{
    public DebugBox()
    {
        var mesh = MeshPrefab.Cube;
        var transform = this.AddComponent(new Transform3d());
        this.AddComponent(new DebugComponent(mesh, transform)
        {
            DrawTransform = true,
            DrawWireframe = true,
            Render = true
        });
    }
}
