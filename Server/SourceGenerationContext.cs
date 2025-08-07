using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(JsonDocument))]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
