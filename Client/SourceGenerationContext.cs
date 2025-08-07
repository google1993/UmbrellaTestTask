using System.Text.Json.Serialization;

namespace Client
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Configuration))]
    internal partial class SourceGenerationContext : JsonSerializerContext { }
}
