using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;
using ajiva.Systems.VulcanEngine.Debug;

namespace ajiva.Entities;

public class DebugBox : DefaultEntity
{
    public DebugBox()
    {
        this.AddComponent(new Transform3d());
        var debugComponent = new DebugComponent
        {
            DrawTransform = true,
            DrawWireframe = true,
            Render = true
        };
        debugComponent.SetMesh(MeshPrefab.Cube);
        this.AddComponent(debugComponent);
    }
}
