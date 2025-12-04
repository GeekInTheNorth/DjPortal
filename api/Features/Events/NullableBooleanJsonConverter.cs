using System.Text.Json;
using System.Text.Json.Serialization;

namespace DjPortalApi.Features.Events;

public sealed class NullableBooleanJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return false;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (bool.TryParse(stringValue, out var result))
            {
                return result;
            }
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            var numberValue = reader.GetInt32();
            return numberValue != 0;
        }

        return reader.GetBoolean();
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}