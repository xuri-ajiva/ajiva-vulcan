using ajiva.Systems.Assets.Contracts;

namespace ajiva.Systems.Assets;

public interface IAssetManager : ISystem
{
    AssetPack AssetPack { get; set; }
    byte[] GetAsset(AssetType assetType, string name);
    Stream GetAssetAsStream(AssetType assetType, string assetName);
}
