using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class OpenAiProjectRecommender : IProjectRecommender
{
    private readonly ChatClient _chatClient;
    private readonly DeterministicProjectRecommender _fallbackRecommender;
    private readonly ILogger<OpenAiProjectRecommender> _logger;

    private const string ProjectJsonSchema = """
    {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "description": { "type": "string" },
        "reason": { "type": "string" },
        "requirements": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "requirement": { "type": "string" },
              "targetsSkill": { "type": "string" },
              "deliverable": { "type": "string" }
            },
            "required": ["requirement", "targetsSkill", "deliverable"],
            "additionalProperties": false
          }
        },
        "expectedDeliverables": {
          "type": "array",
          "items": { "type": "string" }
        }
      },
      "required": ["title", "description", "reason", "requirements", "expectedDeliverables"],
      "additionalProperties": false
    }
    """;

    public OpenAiProjectRecommender(
        string apiKey,
        string model,
        DeterministicProjectRecommender fallbackRecommender,
        ILogger<OpenAiProjectRecommender> logger,
        ChatClient? customChatClient = null)
    {
        _fallbackRecommender = fallbackRecommender ?? throw new ArgumentNullException(nameof(fallbackRecommender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatClient = customChatClient ?? new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<ProjectRecommendationResponse> RecommendAsync(
        RecommendProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleId = request.RoleId?.Trim().ToLowerInvariant() ?? string.Empty;

        try
        {
            var topGaps = request.TopGaps?.Take(3).ToList() ?? new List<string>();
            if (topGaps.Count == 0)
            {
                return await _fallbackRecommender.RecommendAsync(request, cancellationToken);
            }

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(BuildSystemPrompt()),
                new UserChatMessage(BuildUserPrompt(roleId, topGaps, request.Roadmap))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "project_recommendation_response",
                    BinaryData.FromString(ProjectJsonSchema),
                    jsonSchemaFormatDescription: "SkillProof gap-backward real-world project recommendation structured strictly around diagnosed skill gaps",
                    jsonSchemaIsStrict: true
                )
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var rawText = string.Join("", completion.Value.Content.Select(c => c.Text)).Trim();

            var validatedResponse = ValidateAndProcessAiResponse(roleId, rawText, topGaps);
            if (validatedResponse != null)
            {
                return validatedResponse;
            }

            _logger.LogWarning("AI project recommendation validation failed. Invoking deterministic fallback.");
            return await _fallbackRecommender.RecommendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("OpenAI project recommendation failed ({ExceptionType}): {Message}. Invoking deterministic fallback.",
                ex.GetType().Name, ex.Message);

            return await _fallbackRecommender.RecommendAsync(request, cancellationToken);
        }
    }

    public ProjectRecommendationResponse? ValidateAndProcessAiResponse(
        string roleId,
        string rawJson,
        IReadOnlyList<string> expectedTopGaps)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("title", out var titleProp) ||
                !root.TryGetProperty("description", out var descProp) ||
                !root.TryGetProperty("reason", out var reasonProp) ||
                !root.TryGetProperty("requirements", out var reqsElement) ||
                reqsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var title = titleProp.GetString()?.Trim();
            var description = descProp.GetString()?.Trim();
            var reason = reasonProp.GetString()?.Trim();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(reason))
            {
                return null;
            }

            title = SanitizeText(title);
            description = SanitizeText(description);
            reason = SanitizeText(reason);

            var expectedGapsSet = new HashSet<string>(expectedTopGaps, StringComparer.OrdinalIgnoreCase);
            var validatedRequirements = new List<ProjectRequirementDto>();
            var coveredGaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var reqItem in reqsElement.EnumerateArray())
            {
                if (!reqItem.TryGetProperty("requirement", out var reqTextProp) ||
                    !reqItem.TryGetProperty("targetsSkill", out var skillProp) ||
                    !reqItem.TryGetProperty("deliverable", out var delivProp))
                {
                    continue;
                }

                var reqText = reqTextProp.GetString()?.Trim();
                var targetsSkill = skillProp.GetString()?.Trim();
                var deliverable = delivProp.GetString()?.Trim();

                // Validation 1: Targets skill must match one of the diagnosed top gaps
                if (string.IsNullOrWhiteSpace(targetsSkill) || !expectedGapsSet.Contains(targetsSkill))
                {
                    continue; // Discard requirements with invented or unrelated competencies
                }

                // Validation 2: Requirement and deliverable must not be empty
                if (string.IsNullOrWhiteSpace(reqText) || string.IsNullOrWhiteSpace(deliverable))
                {
                    continue;
                }

                reqText = SanitizeText(reqText);
                deliverable = SanitizeText(deliverable);

                validatedRequirements.Add(new ProjectRequirementDto(reqText, targetsSkill, deliverable));
                coveredGaps.Add(targetsSkill);
            }

            if (validatedRequirements.Count == 0)
            {
                return null;
            }

            // Ensure every expected top gap has at least one requirement
            foreach (var gap in expectedTopGaps)
            {
                if (!coveredGaps.Contains(gap))
                {
                    validatedRequirements.Add(new ProjectRequirementDto(
                        $"Implement core technical requirements and architecture patterns targeting {gap}.",
                        gap,
                        $"Working code module, test suite, or technical deliverable demonstrating proficiency in {gap}."
                    ));
                }
            }

            // Extract observable deliverables
            var deliverablesList = new List<string>();
            if (root.TryGetProperty("expectedDeliverables", out var delivsElement) && delivsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in delivsElement.EnumerateArray())
                {
                    var dStr = d.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(dStr))
                    {
                        deliverablesList.Add(SanitizeText(dStr));
                    }
                }
            }

            if (deliverablesList.Count == 0)
            {
                deliverablesList = validatedRequirements
                    .Where(r => !string.IsNullOrWhiteSpace(r.Deliverable))
                    .Select(r => r.Deliverable!)
                    .Distinct()
                    .ToList();
            }

            return new ProjectRecommendationResponse(title, description, reason, validatedRequirements, deliverablesList);
        }
        catch (JsonException)
        {
            return null; // Malformed JSON -> fallback
        }
    }

    private static string SanitizeText(string text)
    {
        // Guardrail: Strip arbitrary percentage scores (e.g. "85%")
        var sanitized = Regex.Replace(text, @"\b\d{1,3}%", "competency-aligned");
        return sanitized.Trim();
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are an expert technical career coach for SkillProof.
        Your mission is to recommend ONE focused, portfolio-worthy real-world project designed BACKWARD from the candidate's diagnosed skill gaps.

        CORE PRINCIPLES:
        1. Design the project BACKWARD from the candidate's diagnosed top skill gaps.
        2. Every major project requirement MUST map to at least one diagnosed top gap (targetsSkill).
        3. Do NOT introduce unrelated competencies or generic tasks simply to make the project larger.
        4. Every requirement MUST specify an observable deliverable artifact (e.g. source code, SQL schema DDL, automated test suite, execution plan analysis, 3-statement model, financial forecast).
        5. The project must be realistic, feasible for a student, and portfolio-worthy.
        6. Do NOT invent candidate experience or claim the candidate has already built these artifacts.
        7. Do NOT include arbitrary numeric percentage scores or hiring guarantees.
        """;
    }

    private static string BuildUserPrompt(
        string roleId,
        IReadOnlyList<string> topGaps,
        IReadOnlyList<RoadmapInputItem>? roadmap)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Target Career Role: {roleId}");
        sb.AppendLine();
        sb.AppendLine("Diagnosed Top Skill Gaps to Address:");
        for (int i = 0; i < topGaps.Count; i++)
        {
            sb.AppendLine($"- Gap {i + 1}: {topGaps[i]}");
        }

        if (roadmap != null && roadmap.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Personalized Learning Roadmap Priorities:");
            foreach (var item in roadmap)
            {
                sb.AppendLine($"- Priority {item.Priority}: {item.Skill}");
                if (!string.IsNullOrWhiteSpace(item.LearningGoal))
                {
                    sb.AppendLine($"  Goal: {item.LearningGoal}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generate ONE focused gap-backward project recommendation conforming strictly to the requested JSON schema.");
        return sb.ToString();
    }
}
