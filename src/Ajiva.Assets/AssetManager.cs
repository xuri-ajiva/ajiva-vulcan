using System.Text.Json;
using Ajiva.Assets.Contracts;
using Ajiva.Utils;
using ProtoBuf;

namespace Ajiva.Assets;

public class AssetManager : SystemBase, IAssetManager
{
    private readonly string assetPath;

    /// <inheritdoc />
    public AssetManager(AjivaConfig config)
    {
        assetPath = config.AssetPath;
        AssetPack = JsonSerializer.Deserialize<AssetPack>(File.ReadAllBytes(assetPath),AssetPackJsonSerializerContext.Default.AssetPack)!;
    }

    public AssetPack? AssetPack { get; set; }

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
