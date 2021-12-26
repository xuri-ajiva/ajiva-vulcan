using ajiva.Components.Media;
using ajiva.Components.Mesh;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Transform;
using ajiva.Models.Instance;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh : DisposingLogger, IComponent
{
    private readonly Transform3d transform;
    public IInstancedMeshInstance? Instance { get; set; }

    public RenderInstanceMesh(IMesh mesh, Transform3d transform, TextureComponent textureComponent)
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

    private void Update(ref MeshInstanceData value)
    {
        value.Position = transform.Position;
        value.Rotation = glm.Radians(transform.Rotation);
        value.Scale = transform.Scale;
        value.TextureIndex = TextureComponent?.TextureId ?? 0;
        value.Padding = vec2.Ones;
    }
}
