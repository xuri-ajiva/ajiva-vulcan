using System.Text.Json.Serialization;

namespace Ajiva.Utils;

[JsonSerializable(typeof(AjivaConfig))]
public partial class AjivaConfigJsonSerializerContext  : JsonSerializerContext
{
    
}
