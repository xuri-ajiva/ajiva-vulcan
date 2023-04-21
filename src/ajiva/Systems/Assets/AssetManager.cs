﻿using Ajiva.Application;
using Ajiva.Systems.Assets.Contracts;
using ProtoBuf;

namespace Ajiva.Systems.Assets;

public class AssetManager : SystemBase, IAssetManager
{
    string assetPath;

    /// <inheritdoc />
    public AssetManager(AjivaConfig config)
    {
        assetPath = config.AssetPath;
        AssetPack = Serializer.Deserialize<AssetPack>(new ReadOnlyMemory<byte>(File.ReadAllBytes(assetPath)));
    }

    public AssetPack AssetPack { get; set; }

    public byte[] GetAsset(AssetType assetType, string name)
    {
        if (AssetPack.Assets.TryGetValue(assetType, out var assets)) return assets.GetAsset(name);

        Log.Error("Asset Not Found, {AssetType}:{name}", assetType, name);
        return Array.Empty<byte>();
    }

    public Stream GetAssetAsStream(AssetType assetType, string assetName)
    {
        var data = GetAsset(assetType, assetName);
        var ms = new MemoryStream(data, false);
        return ms;
    }
}
