using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class OpenAiRoadmapGenerator : IRoadmapGenerator
{
    private readonly ChatClient _chatClient;
    private readonly DeterministicRoadmapGenerator _fallbackGenerator;
    private readonly ILogger<OpenAiRoadmapGenerator> _logger;

    private const string RoadmapJsonSchema = """
    {
      "type": "object",
      "properties": {
        "items": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "skill": { "type": "string" },
              "priority": { "type": "integer" },
              "learningGoal": { "type": "string" },
              "practiceTask": { "type": "string" }
            },
            "required": ["skill", "priority", "learningGoal", "practiceTask"],
            "additionalProperties": false
          }
        }
      },
      "required": ["items"],
      "additionalProperties": false
    }
    """;

    public OpenAiRoadmapGenerator(
        string apiKey,
        string model,
        DeterministicRoadmapGenerator fallbackGenerator,
        ILogger<OpenAiRoadmapGenerator> logger,
        ChatClient? customChatClient = null)
    {
        _fallbackGenerator = fallbackGenerator ?? throw new ArgumentNullException(nameof(fallbackGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatClient = customChatClient ?? new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<RoadmapResponse> GenerateAsync(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleId = request.RoleId?.Trim().ToLowerInvariant() ?? string.Empty;

        try
        {
            var topGaps = request.TopGaps?.Take(3).ToList() ?? new List<string>();
            if (topGaps.Count == 0)
            {
                return await _fallbackGenerator.GenerateAsync(request, cancellationToken);
            }

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(BuildSystemPrompt()),
                new UserChatMessage(BuildUserPrompt(roleId, topGaps, request.Skills))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "roadmap_generation_response",
                    BinaryData.FromString(RoadmapJsonSchema),
                    jsonSchemaFormatDescription: "SkillProof personalized learning roadmap tailored strictly to top diagnosed skill gaps",
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

            _logger.LogWarning("AI roadmap validation failed. Invoking deterministic fallback.");
            return await _fallbackGenerator.GenerateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("OpenAI roadmap generation failed ({ExceptionType}): {Message}. Invoking deterministic fallback.",
                ex.GetType().Name, ex.Message);

            return await _fallbackGenerator.GenerateAsync(request, cancellationToken);
        }
    }

    public RoadmapResponse? ValidateAndProcessAiResponse(
        string roleId,
        string rawJson,
        IReadOnlyList<string> expectedTopGaps)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (!doc.RootElement.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var expectedGapsSet = new HashSet<string>(expectedTopGaps, StringComparer.OrdinalIgnoreCase);
            var itemsBySkill = new Dictionary<string, RoadmapItemDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in itemsElement.EnumerateArray())
            {
                if (!item.TryGetProperty("skill", out var skillProp) ||
                    !item.TryGetProperty("learningGoal", out var goalProp) ||
                    !item.TryGetProperty("practiceTask", out var taskProp))
                {
                    continue;
                }

                var skillName = skillProp.GetString()?.Trim();
                var learningGoal = goalProp.GetString()?.Trim();
                var practiceTask = taskProp.GetString()?.Trim();

                // Validation 1: Must be one of the requested top gaps
                if (string.IsNullOrWhiteSpace(skillName) || !expectedGapsSet.Contains(skillName))
                {
                    continue; // Discard unrelated or invented skills
                }

                // Validation 2: Learning goal and practice task must not be empty
                if (string.IsNullOrWhiteSpace(learningGoal) || string.IsNullOrWhiteSpace(practiceTask))
                {
                    continue;
                }

                learningGoal = SanitizeText(learningGoal);
                practiceTask = SanitizeText(practiceTask);

                itemsBySkill[skillName] = new RoadmapItemDto(
                    skillName,
                    0, // Priority assigned below based on topGaps order
                    learningGoal,
                    practiceTask
                );
            }

            // Build sequential roadmap adhering strictly to the top gaps order
            var validatedItems = new List<RoadmapItemDto>();
            int priority = 1;

            foreach (var gap in expectedTopGaps.Take(3))
            {
                if (itemsBySkill.TryGetValue(gap, out var existingItem))
                {
                    validatedItems.Add(existingItem with { Priority = priority });
                }
                else
                {
                    // Fallback to deterministic item for any omitted gap
                    validatedItems.Add(new RoadmapItemDto(
                        gap,
                        priority,
                        $"Master core principles, architecture patterns, and technical implementation for {gap}.",
                        $"Complete a hands-on project module implementing measurable improvements in {gap}."
                    ));
                }

                priority++;
            }

            return new RoadmapResponse(roleId, validatedItems.Take(3).ToList());
        }
        catch (JsonException)
        {
            return null; // Malformed JSON -> fallback
        }
    }

    private static string SanitizeText(string text)
    {
        // Guardrail: Strip arbitrary percentage numbers (e.g. "85%")
        var sanitized = Regex.Replace(text, @"\b\d{1,3}%", "competency-aligned");
        return sanitized.Trim();
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are an expert technical career coach for SkillProof.
        Your mission is to generate a personalized learning roadmap directly addressing the candidate's diagnosed skill gaps.

        RULES:
        1. Address ONLY the candidate's diagnosed Top Gaps in priority order (1, 2, 3). Maximum 3 priorities.
        2. Do NOT recommend relearning skills where the candidate already demonstrated competency.
        3. Do NOT invent candidate experience, projects, or background.
        4. Do NOT generate arbitrary percentage scores or hiring promises.
        5. For each gap, provide:
           - learningGoal: A concise, highly specific explanation of what concept/principle to master for the target role.
           - practiceTask: A concrete, actionable practice activity with observable deliverables (e.g. "Write an automated test suite mocking...", "Analyze query execution plans on a 10M-row schema...").
        6. Avoid generic advice like "Learn SQL" or "Practice testing". Be specific, technical, and actionable.
        """;
    }

    private static string BuildUserPrompt(
        string roleId,
        IReadOnlyList<string> topGaps,
        IReadOnlyList<RoadmapSkillInput>? skills)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Target Role: {roleId}");
        sb.AppendLine();
        sb.AppendLine("Diagnosed Top Skill Gaps (Prioritize in this exact order):");
        for (int i = 0; i < topGaps.Count; i++)
        {
            sb.AppendLine($"Priority {i + 1}: {topGaps[i]}");
        }

        if (skills != null && skills.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Current Evaluated Skill Profile Context:");
            foreach (var s in skills)
            {
                sb.AppendLine($"- {s.Name}: {s.Level}");
                if (!string.IsNullOrWhiteSpace(s.Reason))
                {
                    sb.AppendLine($"  Reason: {s.Reason}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generate the personalized learning roadmap conforming strictly to the requested JSON schema.");
        return sb.ToString();
    }
}
