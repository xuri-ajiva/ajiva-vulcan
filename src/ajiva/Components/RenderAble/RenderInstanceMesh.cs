using ajiva.Components.Media;
using ajiva.Components.Mesh.Instance;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderInstanceMesh : DisposingLogger, IComponent
{
    public IInstancedMeshInstance Instance { get; }

    public RenderInstanceMesh(IInstancedMeshInstance instance)
    {
        Instance = instance;
    }

    public IChangingObserverOnlyValue<mat4>.OnChangedDelegate OnTransformChange { get; }
    public TextureComponent? TextureComponent { get; set; }

    public void TransformChange(mat4 value)
    {
        Instance.UpdateData(data =>
        {
            data.Value.Position = new vec3(value.m30, value.m31, value.m32);
            data.Value.Rotation = vec3.Zero;
            data.Value.Scale = vec3.Ones;
            data.Value.TextureIndex = TextureComponent?.TextureId ?? 0;
            data.Value.Padding = vec2.Ones;
        });
    }
}
