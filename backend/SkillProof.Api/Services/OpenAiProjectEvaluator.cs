using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class OpenAiProjectEvaluator : IProjectEvaluator
{
    private readonly ChatClient _chatClient;
    private readonly DeterministicProjectEvaluator _fallbackEvaluator;
    private readonly ILogger<OpenAiProjectEvaluator> _logger;

    private const string ProjectEvaluationJsonSchema = """
    {
      "type": "object",
      "properties": {
        "overallStatus": {
          "type": "string",
          "enum": ["Demonstrated", "Partially Demonstrated", "Insufficient Evidence"]
        },
        "skillEvidence": {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "skillId": { "type": "string" },
              "evidenceStatus": {
                "type": "string",
                "enum": ["Demonstrated", "Partially Demonstrated", "Insufficient Evidence"]
              },
              "evidence": {
                "type": "array",
                "items": { "type": "string" }
              },
              "missingEvidence": {
                "type": "array",
                "items": { "type": "string" }
              }
            },
            "required": ["skillId", "evidenceStatus", "evidence", "missingEvidence"],
            "additionalProperties": false
          }
        },
        "demonstratedEvidence": {
          "type": "array",
          "items": { "type": "string" }
        },
        "missingEvidence": {
          "type": "array",
          "items": { "type": "string" }
        },
        "improvementSuggestions": {
          "type": "array",
          "items": { "type": "string" }
        }
      },
      "required": ["overallStatus", "skillEvidence", "demonstratedEvidence", "missingEvidence", "improvementSuggestions"],
      "additionalProperties": false
    }
    """;

    public OpenAiProjectEvaluator(
        string apiKey,
        string model,
        DeterministicProjectEvaluator fallbackEvaluator,
        ILogger<OpenAiProjectEvaluator> logger,
        ChatClient? customChatClient = null)
    {
        _fallbackEvaluator = fallbackEvaluator ?? throw new ArgumentNullException(nameof(fallbackEvaluator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _chatClient = customChatClient ?? new ChatClient(model: model, apiKey: apiKey);
    }

    public async Task<ProjectEvaluationDto> EvaluateAsync(
        GapBasedProjectDto project,
        SubmitProjectEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var combinedEvidence = string.Join(" ", new[]
            {
                request.ProjectSummary,
                request.ImplementationExplanation,
                request.ArchitectureDecisions,
                request.TestingExplanation,
                request.Notes,
                string.Join(" ", request.EvidenceExcerpts ?? new List<string>())
            }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            if (string.IsNullOrWhiteSpace(combinedEvidence) || combinedEvidence.Length < 15)
            {
                return await _fallbackEvaluator.EvaluateAsync(project, request, cancellationToken);
            }

            var messages = new ChatMessage[]
            {
                new SystemChatMessage(BuildSystemPrompt()),
                new UserChatMessage(BuildUserPrompt(project, request))
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    "project_evaluation_response",
                    BinaryData.FromString(ProjectEvaluationJsonSchema),
                    jsonSchemaFormatDescription: "SkillProof qualitative project evidence evaluation",
                    jsonSchemaIsStrict: true
                )
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var rawText = string.Join("", completion.Value.Content.Select(c => c.Text)).Trim();

            var validatedResponse = ValidateAndProcessAiResponse(project, rawText);
            if (validatedResponse != null)
            {
                return validatedResponse;
            }

            _logger.LogWarning("AI project evidence evaluation validation failed. Invoking deterministic fallback.");
            return await _fallbackEvaluator.EvaluateAsync(project, request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("OpenAI project evidence evaluation failed ({ExceptionType}): {Message}. Invoking deterministic fallback.",
                ex.GetType().Name, ex.Message);

            return await _fallbackEvaluator.EvaluateAsync(project, request, cancellationToken);
        }
    }

    public ProjectEvaluationDto? ValidateAndProcessAiResponse(
        GapBasedProjectDto project,
        string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("overallStatus", out var statusProp) ||
                !root.TryGetProperty("skillEvidence", out var skillEvElem) ||
                skillEvElem.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("demonstratedEvidence", out var demElem) ||
                demElem.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("missingEvidence", out var missElem) ||
                missElem.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("improvementSuggestions", out var impElem) ||
                impElem.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var overallStatus = statusProp.GetString() ?? "Partially Demonstrated";
            var validStatuses = new HashSet<string>(new[] { "Demonstrated", "Partially Demonstrated", "Insufficient Evidence" });
            if (!validStatuses.Contains(overallStatus))
            {
                return null;
            }

            var targetSkillIds = new HashSet<string>(project.TargetedSkills.Select(s => s.SkillId), StringComparer.OrdinalIgnoreCase);
            var skillEvidenceResults = new List<SkillEvidenceResultDto>();
            var coveredSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sItem in skillEvElem.EnumerateArray())
            {
                if (!sItem.TryGetProperty("skillId", out var sIdProp) ||
                    !sItem.TryGetProperty("evidenceStatus", out var sStatusProp) ||
                    !sItem.TryGetProperty("evidence", out var evElem) ||
                    !sItem.TryGetProperty("missingEvidence", out var mElem))
                {
                    continue;
                }

                var skillId = sIdProp.GetString()?.Trim() ?? string.Empty;
                var evidenceStatus = sStatusProp.GetString()?.Trim() ?? "Insufficient Evidence";

                if (!targetSkillIds.Contains(skillId) || !validStatuses.Contains(evidenceStatus))
                {
                    continue;
                }

                var evidenceList = evElem.EnumerateArray()
                    .Select(e => SanitizeText(e.GetString() ?? string.Empty))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                var missingList = mElem.EnumerateArray()
                    .Select(m => SanitizeText(m.GetString() ?? string.Empty))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                // Guardrail: Ensure missing evidence uses neutral phrasing
                var sanitizedMissing = new List<string>();
                foreach (var m in missingList)
                {
                    var targetSkill = project.TargetedSkills.FirstOrDefault(ts => ts.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
                    var targetArea = targetSkill?.TargetArea ?? skillId;
                    if (!m.Contains("Insufficient evidence", StringComparison.OrdinalIgnoreCase))
                    {
                        sanitizedMissing.Add($"Insufficient evidence was provided for {targetArea}.");
                    }
                    else
                    {
                        sanitizedMissing.Add(m);
                    }
                }

                skillEvidenceResults.Add(new SkillEvidenceResultDto(skillId, evidenceStatus, evidenceList, sanitizedMissing));
                coveredSkills.Add(skillId);
            }

            // Ensure all target skills are covered
            foreach (var target in project.TargetedSkills)
            {
                if (!coveredSkills.Contains(target.SkillId))
                {
                    skillEvidenceResults.Add(new SkillEvidenceResultDto(
                        SkillId: target.SkillId,
                        EvidenceStatus: "Insufficient Evidence",
                        Evidence: new List<string>(),
                        MissingEvidence: new List<string> { $"Insufficient evidence was provided for {target.TargetArea}." }
                    ));
                }
            }

            var demonstratedEvidence = demElem.EnumerateArray()
                .Select(d => SanitizeText(d.GetString() ?? string.Empty))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var missingEvidence = missElem.EnumerateArray()
                .Select(m => SanitizeText(m.GetString() ?? string.Empty))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var improvementSuggestions = impElem.EnumerateArray()
                .Select(i => SanitizeText(i.GetString() ?? string.Empty))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            // Map requirement results
            var requirementResults = new List<RequirementEvaluationResultDto>();
            foreach (var req in project.Requirements)
            {
                var skillRes = skillEvidenceResults.FirstOrDefault(s => s.SkillId.Equals(req.TargetsSkill, StringComparison.OrdinalIgnoreCase));
                var reqStatus = skillRes?.EvidenceStatus ?? "Insufficient Evidence";
                var notes = reqStatus == "Demonstrated"
                    ? $"Observed verified implementation evidence for {req.TargetsSkill}."
                    : (reqStatus == "Partially Demonstrated"
                        ? $"Partial implementation noted for {req.TargetsSkill}."
                        : $"Insufficient evidence provided for {req.TargetsSkill}.");

                requirementResults.Add(new RequirementEvaluationResultDto(req.Requirement, req.TargetsSkill, reqStatus, notes));
            }

            // Backend Claim Safety Gate
            var portfolioProof = DeterministicProjectEvaluator.GenerateGatedProof(project, skillEvidenceResults);

            return new ProjectEvaluationDto(
                ProjectId: project.ProjectId,
                OverallStatus: overallStatus,
                SkillEvidence: skillEvidenceResults,
                RequirementResults: requirementResults,
                DemonstratedEvidence: demonstratedEvidence,
                MissingEvidence: missingEvidence,
                ImprovementSuggestions: improvementSuggestions,
                PortfolioProof: portfolioProof
            );
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string SanitizeText(string text)
    {
        var sanitized = Regex.Replace(text, @"\b\d{1,3}%", "competency-aligned");
        return sanitized.Trim();
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are an expert technical project evaluator for SkillProof.
        Your mission is to evaluate submitted project evidence strictly against the project's predefined requirements and targeted skills.

        CORE PRINCIPLES:
        1. Evaluate submitted evidence qualitatively: "Demonstrated", "Partially Demonstrated", or "Insufficient Evidence".
        2. Do NOT use numeric scores, percentages, hire-readiness probabilities, or letter grades.
        3. Missing evidence does NOT prove lack of skill: use neutral phrasing such as "Insufficient evidence was provided for [Target Area]".
        4. Do NOT invent completed work or claims not present in the candidate's submission.
        5. Ground every observation in the candidate's actual submitted text.
        """;
    }

    private static string BuildUserPrompt(GapBasedProjectDto project, SubmitProjectEvidenceRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Project Title: {project.Title}");
        sb.AppendLine($"Project Scenario: {project.Scenario}");
        sb.AppendLine();
        sb.AppendLine("Predefined Evaluation Criteria by Skill:");
        foreach (var ts in project.TargetedSkills)
        {
            sb.AppendLine($"- Skill: {ts.SkillId} | Target Area: {ts.TargetArea}");
        }

        sb.AppendLine();
        sb.AppendLine("Candidate Submitted Evidence:");
        if (!string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            sb.AppendLine($"Repository URL: {request.RepositoryUrl}");
        }
        sb.AppendLine($"Project Summary: {request.ProjectSummary}");
        sb.AppendLine($"Implementation Details: {request.ImplementationExplanation}");
        sb.AppendLine($"Architecture & Trade-offs: {request.ArchitectureDecisions}");
        sb.AppendLine($"Testing & Verification: {request.TestingExplanation}");

        if (request.EvidenceExcerpts != null && request.EvidenceExcerpts.Count > 0)
        {
            sb.AppendLine("Code / Artifact Excerpts:");
            foreach (var exc in request.EvidenceExcerpts)
            {
                sb.AppendLine($"  - {exc}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Evaluate the submitted evidence against the predefined project criteria and return strict JSON.");
        return sb.ToString();
    }
}
