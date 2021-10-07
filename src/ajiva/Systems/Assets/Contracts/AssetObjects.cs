﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Ajiva.Wrapper.Logger;
using ProtoBuf;

namespace ajiva.Systems.Assets.Contracts
{
    [ProtoContract]
    public class AssetObjects
    {
        public AssetObjects(AssetType assetType)
        {
            AssetType = assetType;
        }

        [JsonConstructor]
        public AssetObjects() { }

        [ProtoMember(1)]
        public AssetType AssetType { get; }

        [ProtoMember(2)]
        public Dictionary<string, byte[]> Assets { get; set; } = new();

        public void Add(string name, byte[] data)
        {
            var assetName = AssetHelper.AsName(name);
            if (Assets.ContainsKey(assetName))
            {
                LogHelper.Log($"Duplicate AssetName {AssetType}:{assetName}");
            }
            Assets.Add(assetName, data);
        }

        public byte[] GetAsset(string name)
        {
            if (Assets.TryGetValue(AssetHelper.AsName(name), out var data))
            {
                return data;
            }

            LogHelper.Log($"Error: Asset Not Found, {AssetType}:{name}");
            return Array.Empty<byte>();
        }
    }
}
