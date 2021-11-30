using ajiva.Components.Media;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Instance;
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
    public IAChangeAwareBackupBufferOfT<MeshInstanceData>? InstanceData { get; set; }

    public void TransformChange(mat4 value)
    {
        if (Models is null)
        {
            ALog.Warn("RenderMeshUpdate Failed!");
            return;
        }
        else
        {
            
            var data = Models.GetForChange((int)Id);
            data.Value.Model = value;
            data.Value.TextureSamplerId = TextureComponent?.TextureId ?? 0;
        }
        if (InstanceData is null)
        {
            ALog.Warn("RenderMeshUpdate Failed!");
            return;
        }
        else
        {
            
            var data = InstanceData.GetForChange((int)Id);
            data.Value.Position = new vec3(value.m30,value.m31,value.m32);
            data.Value.Rotation = vec3.Zero;
            data.Value.Scale = vec3.Ones;
            data.Value.TextureIndex = TextureComponent?.TextureId ?? 0;
            data.Value.Padding = vec2.Ones;
        }

    }
}
