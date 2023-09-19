using System.Text.Json.Serialization;
using Ajiva.Assets.Contracts;

namespace Ajiva.Assets;

[JsonSerializable(typeof(AssetPack))]
public partial class AssetPackJsonSerializerContext  : JsonSerializerContext
{
    
}
