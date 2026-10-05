using System.Text.Json;
using System.Text.Json.Serialization;
using WellData.Core;

namespace WellData.Api.Json;

/// <summary>Reads and writes a production month as a "YYYY-MM" string.</summary>
public sealed class ProductionMonthJsonConverter : JsonConverter<ProductionMonth>
{
    public override ProductionMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && ProductionMonth.TryParse(reader.GetString(), out var month)
            ? month
            : throw new JsonException("Expected a production month in YYYY-MM format.");

    public override void Write(Utf8JsonWriter writer, ProductionMonth value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
