using ajiva.Components.Media;
using ajiva.Components.Transform;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderMesh3D : RenderMeshIdUnique<RenderMesh3D>
{
    public IChangingObserverOnlyAfter<ITransform<vec3, mat4>, mat4>.OnChangedDelegate OnTransformChange { get; private set; }

    public RenderMesh3D()
    {
        OnTransformChange = TransformChange;
    }

    public TextureComponent? TextureComponent { get; set; }

    private void TransformChange(ITransform<vec3, mat4> _, mat4 after)
    {
        if (Models is null) return;

        var data = Models.GetForChange((int)Id);
        data.Value.Model = after;
        data.Value.TextureSamplerId = TextureComponent?.TextureId ?? 0;
    }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel>? Models { get; set; }
}