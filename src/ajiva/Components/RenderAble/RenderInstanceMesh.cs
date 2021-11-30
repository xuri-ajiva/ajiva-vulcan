using ajiva.Components.Media;
using ajiva.Components.Mesh.Instance;
using ajiva.Components.Transform;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh : DisposingLogger, IComponent
{
    private readonly Transform3d transform;
    public IInstancedMeshInstance Instance { get; }

    public RenderInstanceMesh(IInstancedMeshInstance instance, Transform3d transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += TransformChange;
        Instance = instance;
        TextureComponent = textureComponent;
    }

    public TextureComponent? TextureComponent { get; }

    public void TransformChange(mat4 value)
    {
        Instance.UpdateData(data =>
        {
            data.Value.Position = transform.Position;
            data.Value.Rotation = glm.Radians(transform.Rotation);
            data.Value.Scale = transform.Scale;
            data.Value.TextureIndex = TextureComponent?.TextureId ?? 0;
            data.Value.Padding = vec2.Ones;
        });
    }
}
