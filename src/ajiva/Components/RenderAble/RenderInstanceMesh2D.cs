using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Transform;
using ajiva.Models.Instance;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh2D : DisposingLogger, IComponent
{
    public IInstancedMeshInstance<Mesh2dInstanceData>? Instance { get; set; }

    private readonly Transform2d transform;

    public RenderInstanceMesh2D(IMesh mesh, Transform2d transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += TransformChange;
        Mesh = mesh;
        TextureComponent = textureComponent;
    }

    public IMesh Mesh { get; }
    public TextureComponent? TextureComponent { get; }

    public void TransformChange(mat4 value)
    {
        Instance?.UpdateData(Update);
    }

    private void Update(ref Mesh2dInstanceData value)
    {
        value.Position = transform.Position;
        value.Rotation = glm.Radians(transform.Rotation);
        value.Scale = transform.Scale;
        value.TextureIndex = TextureComponent?.TextureId ?? 0;
        value.Padding = vec2.Ones;
    }
}
