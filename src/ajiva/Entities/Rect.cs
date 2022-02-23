using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform;
using ajiva.Ecs;

namespace ajiva.Entities;

public class Rect : DefaultEntity
{
    public Rect()
    {
        var mesh = MeshPrefab.Rect;
        var transform = this.AddComponent(new Transform2d());
        var textureComponent = this.AddComponent(new TextureComponent { TextureId = 1, });
        this.AddComponent(new RenderInstanceMesh2D(mesh, transform, textureComponent));
    }
}
