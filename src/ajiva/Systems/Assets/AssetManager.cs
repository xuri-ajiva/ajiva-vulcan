using ajiva.Application;
using ajiva.Systems.Assets.Contracts;
using ProtoBuf;

namespace ajiva.Systems.Assets;

public class AssetManager : SystemBase, IAssetManager
{
    string assetPath;

    /// <inheritdoc />
    public AssetManager(Config config)
    {
        assetPath = config.AssetPath;
        AssetPack = Serializer.Deserialize<AssetPack>(new ReadOnlyMemory<byte>(File.ReadAllBytes(assetPath)));
    }

    public AssetPack AssetPack { get; set; }

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
