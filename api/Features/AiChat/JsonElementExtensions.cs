using System.Text.Json;

namespace DjPortalApi.Features.AiChat;

// Tool arguments and Azure's error bodies are both loosely-typed JSON that may be missing properties,
// carry the wrong kind, or not be an object at all, so both are read through these forgiving accessors
// rather than deserialised. Each returns null instead of throwing, including on a default JsonElement.
internal static class JsonElementExtensions
{
    internal static string? GetString(this JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    internal static decimal? GetDecimal(this JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }
}
