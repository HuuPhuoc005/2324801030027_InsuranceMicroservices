using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PricingService.Api.Commands.Dto;

namespace PricingService.Api.Converters;

internal class QuestionAnswerConverter : JsonConverter<QuestionAnswer>
{
    public override QuestionAnswer ReadJson(JsonReader reader, Type objectType, QuestionAnswer? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        return QuestionAnswerJson.Parse(JObject.Load(reader));
    }

    public override void WriteJson(JsonWriter writer, QuestionAnswer? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteStartObject();
        writer.WritePropertyName("questionCode");
        serializer.Serialize(writer, value.QuestionCode);
        writer.WritePropertyName("questionType");
        serializer.Serialize(writer, value.QuestionType);
        writer.WritePropertyName("answer");
        serializer.Serialize(writer, value.GetAnswer());
        writer.WriteEndObject();
    }
}

internal static class QuestionAnswerJson
{
    internal static QuestionAnswer Parse(JObject json)
    {
        var typeToken = json.GetValue("questionType", StringComparison.OrdinalIgnoreCase);
        var code = json.GetValue("questionCode", StringComparison.OrdinalIgnoreCase)?.Value<string>();
        var answer = json.GetValue("answer", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(code) || typeToken is null || answer is null || answer.Type == JTokenType.Null ||
            !Enum.TryParse<QuestionType>(typeToken.ToString(), true, out var type) || !Enum.IsDefined(type))
            throw new JsonSerializationException("A valid questionCode, questionType and answer are required.");

        if (type == QuestionType.Number)
        {
            if (answer.Type is not (JTokenType.Integer or JTokenType.Float))
                throw new JsonSerializationException("Numeric answers must be JSON numbers.");
            return new NumericQuestionAnswer { QuestionCode = code, Answer = answer.Value<decimal>() };
        }
        if (answer.Type != JTokenType.String)
            throw new JsonSerializationException("Text and choice answers must be JSON strings.");
        return type == QuestionType.Text
            ? new TextQuestionAnswer { QuestionCode = code, Answer = answer.Value<string>()! }
            : new ChoiceQuestionAnswer { QuestionCode = code, Answer = answer.Value<string>()! };
    }
}
