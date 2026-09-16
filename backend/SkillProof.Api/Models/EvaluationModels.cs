using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkillProof.Api.Models;

public class FlexibleQuestionIdConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out var longVal))
            {
                return longVal.ToString();
            }
            return reader.GetDouble().ToString(CultureInfo.InvariantCulture);
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }
        throw new JsonException($"Expected string or number for QuestionId, but got {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

public record DiagnosticAnswerSubmission
{
    [JsonConverter(typeof(FlexibleQuestionIdConverter))]
    public string QuestionId { get; init; } = string.Empty;

    public string Answer { get; init; } = string.Empty;

    public DiagnosticAnswerSubmission() { }

    public DiagnosticAnswerSubmission(string questionId, string answer)
    {
        QuestionId = questionId ?? string.Empty;
        Answer = answer ?? string.Empty;
    }

    public DiagnosticAnswerSubmission(int questionId, string answer)
    {
        QuestionId = questionId.ToString();
        Answer = answer ?? string.Empty;
    }

    public void Deconstruct(out string questionId, out string answer)
    {
        questionId = QuestionId;
        answer = Answer;
    }
}

public record EvaluationRequest(
    string RoleId,
    List<DiagnosticAnswerSubmission> Answers
);

public record SkillEvaluationItem(
    string Name,
    string Level,
    string Reason,
    List<string> Evidence
);

public record EvaluationResponse(
    string RoleId,
    List<SkillEvaluationItem> Skills,
    List<string> TopGaps
);
