using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.RenderAble;
using ajiva.Components.Transform.Ui;
using ajiva.Ecs.Entity.Helper;

namespace ajiva.Entities.Ui;

[EntityComponent(typeof(UiTransform), typeof(TextureComponent), typeof(RenderInstanceMesh2D))]
public partial class Rect
{
    protected void InitializeDefault()
    {
        var mesh = MeshPrefab.Rect;
        UiTransform ??= new UiTransform(null,
            UiAnchor.Pixel(10, 20, UiAlignment.Top),
            UiAnchor.Pixel(10, 20, UiAlignment.Left)
        );
        TextureComponent ??= new TextureComponent { TextureId = 1, };
        RenderInstanceMesh2D ??= new RenderInstanceMesh2D(mesh, UiTransform, TextureComponent);
    }
}
