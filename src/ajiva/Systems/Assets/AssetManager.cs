using System;
using System.IO;
using System.Linq;
using ajiva.Application;
using ajiva.Ecs;
using ajiva.Ecs.System;
using ajiva.Ecs.Utils;
using ajiva.Systems.Assets.Contracts;
using ajiva.Systems.VulcanEngine;
using Ajiva.Wrapper.Logger;
using ProtoBuf;

namespace ajiva.Systems.Assets
{
    public class AssetManager : SystemBase, IInit
    {
        /// <inheritdoc />
        public AssetManager(IAjivaEcs ecs) : base(ecs)
        {
        }

        /// <inheritdoc />
        public void Init()
        {
            if (Ecs.TryGetPara<Config>(Const.Default.Config, out var config))
            {
                AssetPack = Serializer.Deserialize<AssetPack>(new ReadOnlyMemory<byte>(File.ReadAllBytes(config.AssetPath)));
            }
        }

        public AssetPack AssetPack { get; set; }

        public byte[] GetAsset(AssetType assetType, string name)
        {
            if (AssetPack.Assets.TryGetValue(assetType, out var assets))
            {
                return assets.GetAsset(name);
            }

            LogHelper.Log($"Error: Asset Not Found, {assetType}:{name}");
            return Array.Empty<byte>();
        }

        public Stream GetAssetAsStream(AssetType assetType, string assetName)
        {
            var data = GetAsset(assetType, assetName);
            var ms = new MemoryStream(data, false);
            return ms;
        }
    }
}
