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

    private const string LegacyRoadmapJsonSchema = """
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

    private const string ExtendedRoadmapJsonSchema = """
    {
      "type": "object",
      "properties": {
        "items": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "skillId": { "type": "string" },
              "skill": { "type": "string" },
              "priority": { "type": "integer" },
              "currentLevel": { "type": "string" },
              "gapType": { "type": "string" },
              "targetArea": { "type": "string" },
              "whyThisMatters": { "type": "string" },
              "learningGoal": { "type": "string" },
              "learningActions": {
                "type": "array",
                "items": { "type": "string" }
              },
              "practiceTask": { "type": "string" },
              "evidenceTarget": { "type": "string" },
              "completionCriteria": {
                "type": "array",
                "items": { "type": "string" }
              }
            },
            "required": [
              "skillId",
              "skill",
              "priority",
              "currentLevel",
              "gapType",
              "targetArea",
              "whyThisMatters",
              "learningGoal",
              "learningActions",
              "practiceTask",
              "evidenceTarget",
              "completionCriteria"
            ],
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
        if (request.HandoffContract != null)
        {
            return await GenerateFromHandoffAsync(request.HandoffContract, request.SessionId, cancellationToken);
        }

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
                new SystemChatMessage(BuildLegacySystemPrompt()),
                new UserChatMessage(BuildLegacyUserPrompt(roleId, topGaps, request.Skills))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "roadmap_generation_response",
                    BinaryData.FromString(LegacyRoadmapJsonSchema),
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

    public async Task<RoadmapResponse> GenerateFromHandoffAsync(
        RoadmapHandoffContract handoff,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (handoff == null || handoff.SkillGaps == null || handoff.SkillGaps.Count == 0)
        {
            return await _fallbackGenerator.GenerateFromHandoffAsync(handoff!, sessionId, cancellationToken);
        }

        var roleId = handoff.RoleId?.Trim().ToLowerInvariant() ?? "backend-developer";

        try
        {
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(BuildHandoffSystemPrompt()),
                new UserChatMessage(BuildHandoffUserPrompt(handoff))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "personalized_roadmap_handoff_response",
                    BinaryData.FromString(ExtendedRoadmapJsonSchema),
                    jsonSchemaFormatDescription: "SkillProof structured personalized learning roadmap from trusted profile handoff",
                    jsonSchemaIsStrict: true
                )
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var rawText = string.Join("", completion.Value.Content.Select(c => c.Text)).Trim();

            var validatedResponse = ValidateAndProcessHandoffAiResponse(roleId, rawText, handoff, sessionId);
            if (validatedResponse != null)
            {
                return validatedResponse;
            }

            _logger.LogWarning("AI handoff roadmap validation failed. Invoking deterministic fallback.");
            return await _fallbackGenerator.GenerateFromHandoffAsync(handoff, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("OpenAI roadmap generation from handoff failed ({ExceptionType}): {Message}. Invoking deterministic fallback.",
                ex.GetType().Name, ex.Message);

            return await _fallbackGenerator.GenerateFromHandoffAsync(handoff, sessionId, cancellationToken);
        }
    }

    public RoadmapResponse? ValidateAndProcessHandoffAiResponse(
        string roleId,
        string rawJson,
        RoadmapHandoffContract handoff,
        string? sessionId = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (!doc.RootElement.TryGetProperty("items", out var itemsElement) ||
                itemsElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var allowedSkillsById = handoff.SkillGaps
                .ToDictionary(g => g.SkillId, g => g, StringComparer.OrdinalIgnoreCase);

            var validatedItems = new List<RoadmapItemDto>();
            int priority = 1;

            foreach (var item in itemsElement.EnumerateArray())
            {
                if (!item.TryGetProperty("skillId", out var skillIdProp) ||
                    !item.TryGetProperty("learningGoal", out var goalProp) ||
                    !item.TryGetProperty("practiceTask", out var taskProp) ||
                    !item.TryGetProperty("evidenceTarget", out var evidenceProp))
                {
                    continue;
                }

                var skillId = skillIdProp.GetString()?.Trim() ?? string.Empty;
                if (!allowedSkillsById.TryGetValue(skillId, out var matchedHandoffSkill))
                {
                    // Strict validation: Reject injected or unknown skills!
                    continue;
                }

                var currentLevel = item.TryGetProperty("currentLevel", out var clProp) ? clProp.GetString()?.Trim() : null;
                // Strict validation: CurrentLevel cannot be replaced by AI!
                if (currentLevel != null && !currentLevel.Equals(matchedHandoffSkill.CurrentLevel, StringComparison.OrdinalIgnoreCase))
                {
                    currentLevel = matchedHandoffSkill.CurrentLevel;
                }
                else
                {
                    currentLevel = matchedHandoffSkill.CurrentLevel;
                }

                var isEvidenceGap = currentLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase);
                var gapType = isEvidenceGap ? "Evidence Gap" : "Development Gap";

                var targetArea = item.TryGetProperty("targetArea", out var taProp) ? taProp.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(targetArea))
                {
                    targetArea = matchedHandoffSkill.DevelopmentAreas.FirstOrDefault() ?? matchedHandoffSkill.SkillName;
                }

                var whyThisMatters = item.TryGetProperty("whyThisMatters", out var whyProp) ? whyProp.GetString()?.Trim() : null;
                if (string.IsNullOrWhiteSpace(whyThisMatters))
                {
                    whyThisMatters = isEvidenceGap
                        ? $"The diagnostic assessment did not collect sufficient evidence to determine your current competency level in {matchedHandoffSkill.SkillName}. This activity provides a direct path to demonstrate practical capability."
                        : $"Your assessment demonstrated {currentLevel} evidence in {matchedHandoffSkill.SkillName}, while deeper {targetArea} reasoning was not yet demonstrated.";
                }

                var learningGoal = goalProp.GetString()?.Trim() ?? string.Empty;
                var practiceTask = taskProp.GetString()?.Trim() ?? string.Empty;
                var evidenceTarget = evidenceProp.GetString()?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(learningGoal) ||
                    string.IsNullOrWhiteSpace(practiceTask) ||
                    string.IsNullOrWhiteSpace(evidenceTarget))
                {
                    continue;
                }

                var learningActions = new List<string>();
                if (item.TryGetProperty("learningActions", out var actionsProp) && actionsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in actionsProp.EnumerateArray())
                    {
                        var text = a.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(text)) learningActions.Add(SanitizeText(text));
                    }
                }

                var completionCriteria = new List<string>();
                if (item.TryGetProperty("completionCriteria", out var criteriaProp) && criteriaProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in criteriaProp.EnumerateArray())
                    {
                        var text = c.GetString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(text)) completionCriteria.Add(SanitizeText(text));
                    }
                }

                validatedItems.Add(new RoadmapItemDto(
                    Skill: matchedHandoffSkill.SkillName,
                    Priority: priority,
                    LearningGoal: SanitizeText(learningGoal),
                    PracticeTask: SanitizeText(practiceTask),
                    SkillId: matchedHandoffSkill.SkillId,
                    CurrentLevel: currentLevel,
                    GapType: gapType,
                    TargetArea: SanitizeText(targetArea),
                    WhyThisMatters: SanitizeText(whyThisMatters),
                    LearningActions: learningActions,
                    EvidenceTarget: SanitizeText(evidenceTarget),
                    CompletionCriteria: completionCriteria
                ));

                priority++;
                if (priority > 5) break; // Maximum 5 items
            }

            if (validatedItems.Count == 0)
            {
                return null;
            }

            var projectTargets = validatedItems.Select(i => new ProjectGapTargetSkill(
                SkillId: i.SkillId ?? i.Skill.ToLowerInvariant(),
                CurrentLevel: i.CurrentLevel ?? "Intermediate",
                TargetArea: i.TargetArea ?? i.Skill,
                PracticeTask: i.PracticeTask,
                EvidenceTarget: i.EvidenceTarget ?? "Technical deliverable"
            )).ToList();

            var projectContext = new ProjectGapContext(roleId, projectTargets);

            return new RoadmapResponse(
                RoleId: roleId,
                Items: validatedItems,
                GeneratedAt: DateTimeOffset.UtcNow,
                SessionId: sessionId,
                ProjectContext: projectContext
            );
        }
        catch (JsonException)
        {
            return null;
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

    private static string BuildLegacySystemPrompt()
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

    private static string BuildLegacyUserPrompt(
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

    private static string BuildHandoffSystemPrompt()
    {
        return """
        You are an expert career-readiness learning architect for SkillProof.
        Your mission is to translate trusted assessment profile gaps into a concise, highly practical, evidence-driven learning roadmap.

        PHILOSOPHY:
        Gap -> Learning Objective -> Focused Learning -> Practice Task -> Evidence Target.
        SkillProof is NOT an LMS. Do NOT create long generic courses.

        RULES:
        1. Address ONLY the provided skills from the trusted handoff in the given priority order. Maximum 5 items.
        2. Distinguish Development Gaps from Evidence Gaps:
           - Development Gap: Evidence was demonstrated, but next-level capability is missing. Focus on deeper mastery.
           - Evidence Gap (Insufficient Evidence): Do NOT assume Beginner. Design an evidence-building diagnostic activity to demonstrate practical ability.
        3. Every item MUST have:
           - targetArea: concise focus area
           - whyThisMatters: neutral, explainable reason connecting assessment to roadmap
           - learningGoal: clear technical objective
           - learningActions: 2-3 focused learning topics
           - practiceTask: concrete hands-on task with realistic scenario
           - evidenceTarget: observable proof to produce (ADR, runnable test suite, benchmark report, code artifact)
           - completionCriteria: 2-3 checklist items
        4. NEVER generate numeric readiness scores, arbitrary percentages, or hiring promises.
        5. NEVER invent unrelated skills or change final skill levels.
        """;
    }

    private static string BuildHandoffUserPrompt(RoadmapHandoffContract handoff)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Role: {handoff.RoleId}");
        sb.AppendLine("Diagnosed Skills & Gaps (Strict order):");

        for (int i = 0; i < handoff.SkillGaps.Count; i++)
        {
            var g = handoff.SkillGaps[i];
            sb.AppendLine($"Priority {i + 1}:");
            sb.AppendLine($"  skillId: {g.SkillId}");
            sb.AppendLine($"  skillName: {g.SkillName}");
            sb.AppendLine($"  currentLevel: {g.CurrentLevel}");
            if (g.DevelopmentAreas != null && g.DevelopmentAreas.Count > 0)
            {
                sb.AppendLine($"  developmentAreas: {string.Join(", ", g.DevelopmentAreas)}");
            }
            if (g.EvidenceGaps != null && g.EvidenceGaps.Count > 0)
            {
                sb.AppendLine($"  evidenceGaps: {string.Join("; ", g.EvidenceGaps)}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generate the structured personalized learning roadmap conforming strictly to the requested JSON schema.");
        return sb.ToString();
    }
}
