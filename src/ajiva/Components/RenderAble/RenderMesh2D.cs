using ajiva.Components.Transform;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Layers.Layer2d;
using ajiva.Utils.Changing;
using GlmSharp;

namespace ajiva.Components.RenderAble;

public class RenderMesh2D : RenderMeshIdUnique<RenderMesh2D>
{
    public IChangingObserverOnlyAfter<ITransform<vec2, mat4>, mat4>.OnChangedDelegate OnTransformChange { get; private set; }

    public RenderMesh2D()
    {
        OnTransformChange = TransformChange;
    }

    private void TransformChange(ITransform<vec2, mat4> sender, mat4 after)
    {
        Models.GetForChange((int)Id).Value.Model = after;
    }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel2d> Models { get; set; }
}