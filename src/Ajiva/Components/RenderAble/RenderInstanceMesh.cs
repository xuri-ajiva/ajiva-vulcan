using System.Numerics;
using Ajiva.Components.Media;
using Ajiva.Components.Mesh;
using Ajiva.Components.Mesh.Instance;
using Ajiva.Components.Transform;
using Ajiva.Models.Instance;

namespace Ajiva.Components.RenderAble;

public class RenderInstanceMesh : DisposingLogger, IComponent
{
    private readonly Transform3d transform;
    public IInstancedMeshInstance<MeshInstanceData>? Instance { get; set; }

    public RenderInstanceMesh(IMesh mesh, Transform3d transform, TextureComponent textureComponent)
    {
        this.transform = transform;
        transform.ChangingObserver.OnChanged += TransformChange;
        Mesh = mesh;
        TextureComponent = textureComponent;
    }

    public IMesh Mesh { get; }
    public TextureComponent? TextureComponent { get; }

    public void TransformChange(Matrix4x4 value)
    {
        Instance?.UpdateData(Update);
    }

    private void Update(ref MeshInstanceData value)
    {
        value.Position = transform.Position;
        value.Rotation = transform.Rotation; // todo check if radians are needed here
        value.Scale = transform.Scale;
        value.TextureIndex = TextureComponent?.TextureId ?? 0;
        value.Padding = Vector2.One;
    }
}
