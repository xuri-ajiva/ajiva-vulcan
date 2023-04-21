using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.RenderAble;
using Ajiva.Components.Transform.Ui;
using Ajiva.Ecs.Entity.Helper;

namespace Ajiva.Entities.Ui;

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
        TextureComponent ??= new TextureComponent {
            TextureId = 1
        };
        RenderInstanceMesh2D ??= new RenderInstanceMesh2D(mesh, UiTransform, TextureComponent);
    }
}