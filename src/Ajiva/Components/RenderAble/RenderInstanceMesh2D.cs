using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.Mesh.Instance;
using Ajiva.Components.Transform.Ui;
using Ajiva.Models.Instance;

namespace Ajiva.Components.RenderAble;

public class RenderInstanceMesh2D : DisposingLogger, IComponent
{
    private readonly TextureComponent? textureComponent;

    private readonly UiTransform transform;

    public RenderInstanceMesh2D(IMesh mesh, UiTransform transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += sender => UpdateData();
        Mesh = mesh;
        this.textureComponent = textureComponent;
    }

    public IInstancedMeshInstance<UiInstanceData>? Instance { get; set; }

    public IMesh Mesh { get; }

    public void UpdateData()
    {
        Instance?.UpdateData(Update);
    }

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