using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceDesk.Web.Models.Tickets;

// Convierte "Open"/"InProgress"/"Closed" <-> 0/1/2 (acepta también números)
public sealed class TicketStatusIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()?.Trim() ?? "";
            return s.ToLowerInvariant() switch
            {
                "open" => 0,
                "inprogress" => 1,
                "in_progress" => 1,
                "in progress" => 1,
                "closed" => 2,
                _ => 0 // default seguro
            };
        }

        throw new JsonException($"Status inválido. Token: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}

// Convierte "P1"/"P2"/"P3" <-> 0/1/2 (acepta también números)
public sealed class TicketPriorityIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()?.Trim().ToUpperInvariant() ?? "";
            return s switch
            {
                "P1" => 0,
                "P2" => 1,
                "P3" => 2,
                _ => 1 // default P2
            };
        }

        throw new JsonException($"Priority inválido. Token: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value);
}
