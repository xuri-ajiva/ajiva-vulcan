using ajiva.Components.Mesh;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;

namespace ajiva.Entities;

public class Rect : DefaultEntity
{
    public Rect()
    {
        this.AddComponent(new Transform2d());
        var renderMesh = new RenderMesh2D
        {
            Render = true
        };
        renderMesh.SetMesh(MeshPrefab.Rect);

        this.AddComponent(renderMesh);
    }
}
