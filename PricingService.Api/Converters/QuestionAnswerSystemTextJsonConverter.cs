using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;
using PricingService.Api.Commands.Dto;

namespace PricingService.Api.Converters;

// Keep the supplied Newtonsoft API contract compatible with Alba and HttpClient's serializer.
public sealed class QuestionAnswerSystemTextJsonConverter : JsonConverter<QuestionAnswer>
{
    public override QuestionAnswer Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        try { return QuestionAnswerJson.Parse(JObject.Parse(document.RootElement.GetRawText())); }
        catch (Newtonsoft.Json.JsonException error) { throw new JsonException(error.Message, error); }
    }

    public override void Write(Utf8JsonWriter writer, QuestionAnswer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("questionCode", value.QuestionCode);
        writer.WriteNumber("questionType", (int)value.QuestionType);
        writer.WritePropertyName("answer");
        JsonSerializer.Serialize(writer, value.GetAnswer(), options);
        writer.WriteEndObject();
    }
}
