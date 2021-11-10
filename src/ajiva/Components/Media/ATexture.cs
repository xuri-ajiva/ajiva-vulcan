﻿using SharpVk;

namespace ajiva.Components.Media;

public partial class ATexture : DisposingLogger
{
    public ATexture()
    {
        TextureId = INextId<ATexture>.Next();
    }

    public uint TextureId { get; }
    public Sampler Sampler { get; set; } = null!;

    public AImage Image { get; set; } = null!;

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources(bool disposing)
    {
        Sampler.Dispose();
        Image.Dispose();
        INextId<ATexture>.Remove(TextureId);
    }

    public DescriptorImageInfo DescriptorImageInfo => new() {Sampler = Sampler, ImageView = Image.View, ImageLayout = ImageLayout.ShaderReadOnlyOptimal};
}