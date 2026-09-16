using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SkillProof.Api.Data;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class OpenAiDiagnosticEvaluator : IDiagnosticEvaluator
{
    private readonly ChatClient _chatClient;
    private readonly DeterministicDiagnosticEvaluator _fallbackEvaluator;
    private readonly ILogger<OpenAiDiagnosticEvaluator> _logger;

    private static readonly HashSet<string> ApprovedLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Beginner",
        "Intermediate",
        "Advanced",
        "Insufficient Evidence"
    };

    private const string EvaluationJsonSchema = """
    {
      "type": "object",
      "properties": {
        "evaluations": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "competency": { "type": "string" },
              "level": {
                "type": "string",
                "enum": ["Beginner", "Intermediate", "Advanced", "Insufficient Evidence"]
              },
              "reason": { "type": "string" },
              "evidence": {
                "type": "array",
                "items": { "type": "string" }
              }
            },
            "required": ["competency", "level", "reason", "evidence"],
            "additionalProperties": false
          }
        }
      },
      "required": ["evaluations"],
      "additionalProperties": false
    }
    """;

    public OpenAiDiagnosticEvaluator(
        string apiKey,
        string model,
        DeterministicDiagnosticEvaluator fallbackEvaluator,
        ILogger<OpenAiDiagnosticEvaluator> logger,
        ChatClient? customChatClient = null)
    {
        _fallbackEvaluator = fallbackEvaluator ?? throw new ArgumentNullException(nameof(fallbackEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatClient = customChatClient ?? new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<EvaluationResponse> EvaluateAsync(
        string roleId,
        IReadOnlyList<DiagnosticAnswerSubmission> submissions,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleId = roleId.Trim().ToLowerInvariant();

        try
        {
            var roleQuestions = SeedData.Questions
                .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var isDynamicSqlite = submissions.Any(s => s.QuestionId.StartsWith("q-be-", StringComparison.OrdinalIgnoreCase));
            if (isDynamicSqlite || roleQuestions.Count == 0)
            {
                return await _fallbackEvaluator.EvaluateAsync(roleId, submissions, cancellationToken);
            }

            var answersByQuestionId = submissions
                .Where(a => int.TryParse(a.QuestionId, out _))
                .GroupBy(a => int.Parse(a.QuestionId))
                .ToDictionary(g => g.Key, g => g.Last().Answer);

            var userPrompt = BuildUserPrompt(normalizedRoleId, roleQuestions, answersByQuestionId);

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(BuildSystemPrompt()),
                new UserChatMessage(userPrompt)
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "diagnostic_evaluation_response",
                    BinaryData.FromString(EvaluationJsonSchema),
                    jsonSchemaFormatDescription: "SkillProof qualitative diagnostic evaluation output conforming to 4 approved levels",
                    jsonSchemaIsStrict: true
                )
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var rawText = string.Join("", completion.Value.Content.Select(c => c.Text)).Trim();

            var validatedResponse = ValidateAndProcessAiResponse(normalizedRoleId, rawText, roleQuestions, answersByQuestionId);
            if (validatedResponse != null)
            {
                return validatedResponse;
            }

            _logger.LogWarning("AI output validation failed. Using deterministic fallback.");
            return await _fallbackEvaluator.EvaluateAsync(roleId, submissions, cancellationToken);
        }
        catch (Exception ex)
        {
            // Catch any OpenAI runtime failures: timeout, rate limit, auth, network, etc.
            // Never log secrets, authorization headers, or private credentials
            _logger.LogWarning("OpenAI evaluation failed ({ExceptionType}): {Message}. Invoking deterministic fallback.",
                ex.GetType().Name, ex.Message);

            return await _fallbackEvaluator.EvaluateAsync(roleId, submissions, cancellationToken);
        }
    }

    public EvaluationResponse? ValidateAndProcessAiResponse(
        string roleId,
        string rawJson,
        IReadOnlyList<InternalQuestion> roleQuestions,
        IReadOnlyDictionary<int, string> answersByQuestionId)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (!doc.RootElement.TryGetProperty("evaluations", out var evaluationsElement) ||
                evaluationsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var expectedCompetencies = roleQuestions.ToDictionary(q => q.Competency, q => q, StringComparer.OrdinalIgnoreCase);
            var aiEvaluationsByCompetency = new Dictionary<string, (string Level, string Reason, List<string> Evidence)>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in evaluationsElement.EnumerateArray())
            {
                if (!item.TryGetProperty("competency", out var compProp) ||
                    !item.TryGetProperty("level", out var levelProp) ||
                    !item.TryGetProperty("reason", out var reasonProp) ||
                    !item.TryGetProperty("evidence", out var evidenceProp))
                {
                    continue;
                }

                var competencyName = compProp.GetString()?.Trim();
                var rawLevel = levelProp.GetString()?.Trim();
                var rawReason = reasonProp.GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(competencyName) || !expectedCompetencies.ContainsKey(competencyName))
                {
                    continue; // Skip unrecognized or invented competencies
                }

                // Strict 4-tier qualitative level enforcement
                string validatedLevel = ApprovedLevels.Contains(rawLevel ?? string.Empty)
                    ? rawLevel!
                    : "Insufficient Evidence";

                // Reason validation: must not be empty, strip any accidental numeric percentages
                string validatedReason = string.IsNullOrWhiteSpace(rawReason)
                    ? $"Evaluation based on rubric criteria for {competencyName}."
                    : SanitizeReason(rawReason);

                // Evidence extraction: must be a list of non-empty strings
                var evidenceList = new List<string>();
                if (evidenceProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var ev in evidenceProp.EnumerateArray())
                    {
                        var evStr = ev.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(evStr))
                        {
                            evidenceList.Add(evStr);
                        }
                    }
                }

                aiEvaluationsByCompetency[competencyName] = (validatedLevel, validatedReason, evidenceList);
            }

            var validatedSkillItems = new List<SkillEvaluationItem>();

            foreach (var q in roleQuestions)
            {
                if (aiEvaluationsByCompetency.TryGetValue(q.Competency, out var aiEval))
                {
                    validatedSkillItems.Add(new SkillEvaluationItem(q.Competency, aiEval.Level, aiEval.Reason, aiEval.Evidence));
                }
                else
                {
                    // If a competency was omitted by the AI, fall back safely for this item
                    answersByQuestionId.TryGetValue(q.Id, out var ans);
                    bool isBlank = string.IsNullOrWhiteSpace(ans) || ans.Trim().Length < 15;
                    validatedSkillItems.Add(new SkillEvaluationItem(
                        q.Competency,
                        isBlank ? "Insufficient Evidence" : "Beginner",
                        isBlank
                            ? $"The answer submitted for {q.Competency} does not provide enough relevant evidence to assign a competency level."
                            : $"Demonstrates foundational awareness of {q.Competency}.",
                        new List<string>()
                    ));
                }
            }

            var topGaps = DeterministicDiagnosticEvaluator.CalculateTopGaps(validatedSkillItems);
            return new EvaluationResponse(roleId, validatedSkillItems, topGaps);
        }
        catch (JsonException)
        {
            return null; // Malformed JSON -> will invoke fallback
        }
    }

    private static string SanitizeReason(string reason)
    {
        // Guardrail: remove arbitrary percentage scores (e.g. "scored 95%" or "85% proficiency")
        var sanitized = Regex.Replace(reason, @"\b\d{1,3}%", "rubric-aligned");
        return sanitized.Trim();
    }

    public static string BuildSystemPrompt() => DiagnosticEvaluationPromptBuilder.BuildSystemPrompt();

    public static string BuildUserPrompt(
        string roleId,
        IReadOnlyList<InternalQuestion> roleQuestions,
        IReadOnlyDictionary<int, string> answersByQuestionId) =>
        DiagnosticEvaluationPromptBuilder.BuildMultiQuestionUserPrompt(roleId, roleQuestions, answersByQuestionId);
}
