using System.Text.Json.Serialization;

namespace EchoPatcher
{
    [JsonSourceGenerationOptions(WriteIndented = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )]
    [JsonSerializable(typeof(ModdedTag))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
