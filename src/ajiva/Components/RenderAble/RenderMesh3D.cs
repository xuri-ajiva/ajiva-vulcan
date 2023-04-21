﻿using System.Numerics;
using ajiva.Components.Media;
using ajiva.Models.Buffer.ChangeAware;
using ajiva.Models.Instance;
using ajiva.Models.Layers.Layer3d;
using ajiva.Utils.Changing;

namespace ajiva.Components.RenderAble;

public class RenderMesh3D : RenderMeshIdUnique<RenderMesh3D>
{
    public RenderMesh3D()
    {
        OnTransformChange = TransformChange;
    }

    public IChangingObserverOnlyValue<Matrix4x4>.OnChangedDelegate OnTransformChange { get; }

    public TextureComponent? TextureComponent { get; set; }

    public IAChangeAwareBackupBufferOfT<SolidUniformModel>? Models { get; set; }
    public IAChangeAwareBackupBufferOfT<MeshInstanceData>? InstanceData { get; set; }

    public void TransformChange(Matrix4x4 value)
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
            data.Value.Position = value.Translation;
            data.Value.Rotation = Vector3.Zero;
            data.Value.Scale = Vector3.One;
            data.Value.TextureIndex = TextureComponent?.TextureId ?? 0;
            data.Value.Padding = Vector2.One;
        }

    }
}
