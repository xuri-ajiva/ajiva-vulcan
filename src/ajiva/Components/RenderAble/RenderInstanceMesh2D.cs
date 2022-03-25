using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Transform.Ui;
using ajiva.Models.Instance;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh2D : DisposingLogger, IComponent
{
    public IInstancedMeshInstance<UiInstanceData>? Instance { get; set; }

    private readonly UiTransform transform;
    private readonly TextureComponent? textureComponent;

    public RenderInstanceMesh2D(IMesh mesh, UiTransform transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += sender => UpdateData();
        Mesh = mesh;
        this.textureComponent = textureComponent;
    }

    public IMesh Mesh { get; }

    public void UpdateData() => Instance?.UpdateData(Update);

    private void Update(ref UiInstanceData value)
    {
        value.Offset = transform.RenderSize.Min;
        value.Scale = transform.RenderSize.Max - transform.RenderSize.Min;
        //(value.Offset, value.Scale) = transform.CalculateRenderOffsetScale(extent);
        value.Rotation = transform.Rotation;
        value.TextureIndex = textureComponent?.TextureId ?? 0;
        value.DrawType = UiDrawType.TexturedRectangle;
    }
}
