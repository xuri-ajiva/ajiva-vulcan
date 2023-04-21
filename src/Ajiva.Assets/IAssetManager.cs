using Ajiva.Assets.Contracts;

namespace Ajiva.Assets;

public interface IAssetManager : ISystem
{
    AssetPack AssetPack { get; set; }
    byte[] GetAsset(AssetType assetType, string name);
    Stream GetAssetAsStream(AssetType assetType, string assetName);
}