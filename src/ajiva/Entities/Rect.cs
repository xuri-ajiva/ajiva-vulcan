using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs;

namespace ajiva.Entities;

public class Rect : DefaultEntity
{
    public Rect()
    {
        var mesh = MeshPrefab.Rect;
        var transform = this.AddComponent(new UiTransform(
            UiAnchor.Pixel(10, 20, UiAlignment.Top),
            UiAnchor.Pixel(10, 20, UiAlignment.Left)
        ));
        var textureComponent = this.AddComponent(new TextureComponent { TextureId = 1, });
        this.AddComponent(new RenderInstanceMesh2D(mesh, transform, textureComponent));
    }
}
