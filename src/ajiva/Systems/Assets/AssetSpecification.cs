using ajiva.Systems.Assets.Contracts;

namespace ajiva.Systems.Assets;

public class AssetSpecification
{
    public DirectoryInfo Root { get; }
    public Dictionary<AssetType, string> PathMap { get; }

    public AssetSpecification(string root, Dictionary<AssetType, string> pathMap)
    {
        Root = new DirectoryInfo(root);
        PathMap = pathMap;
    }

    public DirectoryInfo Get(AssetType type)
    {
        if (!PathMap.ContainsKey(type))
            throw new Exception("Type Not existent");
        return new DirectoryInfo(Path.Combine(Root.FullName, PathMap[type]));
    }
}