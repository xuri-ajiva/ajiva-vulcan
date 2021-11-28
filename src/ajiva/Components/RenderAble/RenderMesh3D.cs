using ajiva.Components.Media;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer3d;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderMesh3D : RenderMeshIdUnique<RenderMesh3D>
{
    public RenderMesh3D()
    {
        OnTransformChange = TransformChange;
    }

    public IChangingObserverOnlyValue<mat4>.OnChangedDelegate OnTransformChange { get; }

    public TextureComponent? TextureComponent { get; set; }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel>? Models { get; set; }

    public void TransformChange(mat4 value)
    {
        if (Models is null)
        {
            ALog.Warn("RenderMeshUpdate Failed!");
            return;
        }

        var data = Models.GetForChange((int)Id);
        data.Value.Model = value;
        data.Value.TextureSamplerId = TextureComponent?.TextureId ?? 0;
    }
}
