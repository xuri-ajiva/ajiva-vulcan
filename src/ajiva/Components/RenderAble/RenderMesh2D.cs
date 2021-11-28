using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderMesh2D : RenderMeshIdUnique<RenderMesh2D>
{
    public RenderMesh2D()
    {
        OnTransformChange = TransformChange;
    }

    public IChangingObserverOnlyValue<mat4>.OnChangedDelegate OnTransformChange { get; }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel2d> Models { get; set; }

    public void TransformChange(mat4 value)
    {
        Models.GetForChange((int)Id).Value.Model = value;
    }
}
