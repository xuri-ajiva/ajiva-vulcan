using ProtoBuf;

namespace Ajiva.Assets.Contracts;

[ProtoContract]
public class AssetPack
{
    private readonly object @lock = new object();

    [ProtoMember(1)]
    public Dictionary<AssetType, AssetObjects> Assets { get; set; } = new Dictionary<AssetType, AssetObjects>();

    public void Add(AssetType assetType, string name, byte[] data)
    {
        lock (@lock)
        {
            if (!Assets.ContainsKey(assetType)) Assets.Add(assetType, new AssetObjects(assetType));
            Assets[assetType].Add(name, data);
        }
    }

    public void Add(AssetType assetType, string relPathName, FileInfo fileInfo)
    {
        Add(assetType, AssetHelper.Combine(relPathName, fileInfo.Name), File.ReadAllBytes(fileInfo.FullName));
    }
}