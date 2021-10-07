using System.Collections.Generic;
using System.IO;
using ProtoBuf;

namespace ajiva.Systems.Assets.Contracts
{
    [ProtoContract]
    public class AssetPack
    {
        [ProtoMember(1)]
        public Dictionary<AssetType, AssetObjects> Assets { get; set; } = new();

        private object @lock = new object();

        public void Add(AssetType assetType, string name, byte[] data)
        {
            lock (@lock)
            {
                if (!Assets.ContainsKey(assetType))
                {
                    Assets.Add(assetType, new AssetObjects(assetType));
                }
                Assets[assetType].Add(name, data);
            }
        }

        public void Add(AssetType assetType, string relPathName, FileInfo fileInfo)
        {
            Add(assetType, AssetHelper.Combine(relPathName, fileInfo.Name), File.ReadAllBytes(fileInfo.FullName));
        }
    }
}