using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Transform.Ui;
using ajiva.Models.Instance;
using SharpVk;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh2D : DisposingLogger, IComponent
{
    public IInstancedMeshInstance<UiInstanceData>? Instance { get; set; }

    private readonly UiTransform transform;
    private readonly TextureComponent? textureComponent;
    private Extent2D extent;

    public RenderInstanceMesh2D(IMesh mesh, UiTransform transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += sender => UpdateData();
        Mesh = mesh;
        this.textureComponent = textureComponent;
    }

    public IMesh Mesh { get; }
    public Extent2D Extent
    {
        get => extent;
        set
        {
            extent = value;
            UpdateData();
        }
    }

    private void UpdateData() => Instance?.UpdateData(Update);

    private void Update(ref UiInstanceData value)
    {
        value.PosCombine = transform.GetRenderPoints(extent);
        value.Rotation = transform.Rotation;
        value.TextureIndex = textureComponent?.TextureId ?? 0;
        value.DrawType = UiDrawType.TexturedRectangle;
    }
}
