﻿using ajiva.Application;
using ajiva.Ecs;
using ajiva.Systems.Assets.Contracts;
using ProtoBuf;

namespace ajiva.Systems.Assets;

public class AssetManager : SystemBase, IInit
{
    /// <inheritdoc />
    public AssetManager(IAjivaEcs ecs) : base(ecs)
    {
    }

    public AssetPack AssetPack { get; set; }

    /// <inheritdoc />
    public void Init()
    {
        if (Ecs.TryGetPara<Config>(Const.Default.Config, out var config)) AssetPack = Serializer.Deserialize<AssetPack>(new ReadOnlyMemory<byte>(File.ReadAllBytes(config.AssetPath)));
    }

    public byte[] GetAsset(AssetType assetType, string name)
    {
        if (AssetPack.Assets.TryGetValue(assetType, out var assets)) return assets.GetAsset(name);

        ALog.Error($"Asset Not Found, {assetType}:{name}");
        return Array.Empty<byte>();
    }

    public Stream GetAssetAsStream(AssetType assetType, string assetName)
    {
        var data = GetAsset(assetType, assetName);
        var ms = new MemoryStream(data, false);
        return ms;
    }
}