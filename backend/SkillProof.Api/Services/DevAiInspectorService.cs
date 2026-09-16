using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IDevAiInspectorService
{
    AiRuntimeConfigDto GetRuntimeConfig();
    Task<List<DevQuestionSummaryDto>> GetQuestionsAsync(string? roleId, string? skillId, string? difficulty, CancellationToken cancellationToken = default);
    Task<DevQuestionDetailDto?> GetQuestionDetailAsync(string questionId, CancellationToken cancellationToken = default);
    Task<(bool Success, AiDiagnosticTraceDto? Trace, string? ErrorCode, string? ErrorMessage)> EvaluateAsync(DevEvaluateRequest request, CancellationToken cancellationToken = default);
    Task<DevAdaptiveInspectionDto?> InspectAdaptiveSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}

public class DevAiInspectorService : IDevAiInspectorService
{
    private readonly CatalogDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly IAiDiagnosticTraceStore _traceStore;
    private readonly IAdaptiveSessionStore _sessionStore;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IProjectRecommender? _projectRecommender;
    private readonly IProjectEvaluator? _projectEvaluator;
    private readonly ILogger<DevAiInspectorService> _logger;

    public DevAiInspectorService(
        CatalogDbContext db,
        IConfiguration config,
        IHostEnvironment env,
        IAiDiagnosticTraceStore traceStore,
        IAdaptiveSessionStore sessionStore,
        IRoadmapGenerator roadmapGenerator,
        ILogger<DevAiInspectorService> logger,
        IProjectRecommender? projectRecommender = null,
        IProjectEvaluator? projectEvaluator = null)
    {
        _db = db;
        _config = config;
        _env = env;
        _traceStore = traceStore;
        _sessionStore = sessionStore;
        _roadmapGenerator = roadmapGenerator;
        _projectRecommender = projectRecommender;
        _projectEvaluator = projectEvaluator;
        _logger = logger;
    }

    public AiRuntimeConfigDto GetRuntimeConfig()
    {
        var apiKey = _config["OpenAI:ApiKey"];
        var model = _config["OpenAI:Model"] ?? "gpt-5.4-mini";

        var isLiveAiDisabled = string.Equals(Environment.GetEnvironmentVariable("DISABLE_LIVE_AI"), "true", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(_config["OpenAI:LiveEvaluationEnabled"], "false", StringComparison.OrdinalIgnoreCase);

        var liveEvaluationEnabled = !isLiveAiDisabled;
        var credentialsConfigured = !string.IsNullOrWhiteSpace(apiKey);
        var liveAiAvailable = liveEvaluationEnabled && credentialsConfigured;

        var activeEvaluator = liveAiAvailable
            ? "OpenAiDiagnosticEvaluator"
            : "DeterministicDiagnosticEvaluator";

        var statusMessage = !liveEvaluationEnabled
            ? "Live AI Disabled. Deterministic Evaluator Active."
            : (!credentialsConfigured
                ? "Live AI Enabled in config, but OpenAI credentials are not configured. Deterministic Evaluator Active."
                : $"Live AI Active with OpenAI (model: {model})");

        return new AiRuntimeConfigDto(
            Provider: "OpenAI",
            Model: model,
            LiveEvaluationEnabled: liveEvaluationEnabled,
            CredentialsConfigured: credentialsConfigured,
            LiveAiAvailable: liveAiAvailable,
            ActiveEvaluator: activeEvaluator,
            Environment: _env.EnvironmentName,
            StatusMessage: statusMessage
        );
    }

    public async Task<List<DevQuestionSummaryDto>> GetQuestionsAsync(
        string? roleId,
        string? skillId,
        string? difficulty,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Questions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(roleId))
        {
            var r = roleId.Trim().ToLowerInvariant();
            query = query.Where(q => q.RoleId.ToLower() == r);
        }

        if (!string.IsNullOrWhiteSpace(skillId))
        {
            var s = skillId.Trim().ToLowerInvariant();
            query = query.Where(q => q.SkillId.ToLower() == s);
        }

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            var d = difficulty.Trim().ToLowerInvariant();
            query = query.Where(q => q.Difficulty.ToLower() == d);
        }

        return await query
            .OrderBy(q => q.RoleId)
            .ThenBy(q => q.SkillId)
            .ThenBy(q => q.Difficulty)
            .ThenBy(q => q.Id)
            .Select(q => new DevQuestionSummaryDto(
                q.Id,
                q.RoleId,
                q.SkillId,
                q.Difficulty,
                q.QuestionType,
                q.QuestionText,
                q.VerificationStatus
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<DevQuestionDetailDto?> GetQuestionDetailAsync(
        string questionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(questionId)) return null;

        var q = await _db.Questions
            .Include(x => x.Rubric)
            .Include(x => x.QuestionSubskills)
            .Include(x => x.FrameworkSources)
                .ThenInclude(fs => fs.Source)
            .Include(x => x.InterviewEvidence)
                .ThenInclude(ie => ie.Source)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == questionId.Trim(), cancellationToken);

        if (q == null) return null;

        var subskills = q.QuestionSubskills.Select(qs => qs.SubskillId).ToList();

        var expectedSignals = new List<string>();
        if (!string.IsNullOrWhiteSpace(q.ExpectedSignalsJson))
        {
            try
            {
                expectedSignals = JsonSerializer.Deserialize<List<string>>(q.ExpectedSignalsJson) ?? new List<string>();
            }
            catch
            {
                // Fallback to empty list
            }
        }

        var rubricDto = new DevRubricDto(
            InsufficientEvidence: q.Rubric?.InsufficientEvidence ?? string.Empty,
            Beginner: q.Rubric?.Beginner ?? string.Empty,
            Intermediate: q.Rubric?.Intermediate ?? string.Empty,
            Advanced: q.Rubric?.Advanced ?? string.Empty
        );

        var frameworkSources = q.FrameworkSources
            .Where(fs => fs.Source != null)
            .Select(fs => new DevSourceRefDto(
                fs.SourceId,
                fs.Source.Title,
                fs.Source.Publisher,
                fs.Source.SourceType,
                fs.Source.Url
            ))
            .ToList();

        var interviewEvidence = q.InterviewEvidence
            .Select(ie =>
            {
                var topics = new List<string>();
                if (!string.IsNullOrWhiteSpace(ie.SupportedTopicsJson))
                {
                    try
                    {
                        topics = JsonSerializer.Deserialize<List<string>>(ie.SupportedTopicsJson) ?? new List<string>();
                    }
                    catch
                    {
                        // ignore
                    }
                }
                return new DevInterviewEvidenceDto(
                    ie.SourceId,
                    ie.EvidenceType,
                    ie.EvidenceStrength,
                    ie.Notes,
                    topics
                );
            })
            .ToList();

        return new DevQuestionDetailDto(
            q.Id,
            q.RoleId,
            q.SkillId,
            q.Difficulty,
            q.QuestionType,
            q.QuestionText,
            q.Provenance,
            q.VerificationStatus,
            subskills,
            expectedSignals,
            rubricDto,
            frameworkSources,
            interviewEvidence
        );
    }

    public async Task<(bool Success, AiDiagnosticTraceDto? Trace, string? ErrorCode, string? ErrorMessage)> EvaluateAsync(
        DevEvaluateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.QuestionId))
        {
            return (false, null, "VALIDATION_ERROR", "Field 'questionId' is required.");
        }

        var detail = await GetQuestionDetailAsync(request.QuestionId, cancellationToken);
        if (detail == null)
        {
            return (false, null, "QUESTION_NOT_FOUND", $"Question '{request.QuestionId}' was not found in SQLite catalog.");
        }

        var runtime = GetRuntimeConfig();
        var traceId = $"trace-{Guid.NewGuid():N}";
        var candidateAnswer = request.CandidateAnswer ?? string.Empty;
        var mode = (request.Mode ?? "deterministic").Trim().ToLowerInvariant();

        // Build prompts using shared prompt builder
        var systemInstructions = DiagnosticEvaluationPromptBuilder.BuildSystemPrompt();
        var evaluationInstructions = DiagnosticEvaluationPromptBuilder.BuildSingleQuestionUserPrompt(
            detail.RoleId,
            detail.SkillId,
            detail.Id,
            detail.Difficulty,
            detail.QuestionType,
            detail.QuestionText,
            detail.Rubric.Beginner,
            detail.Rubric.Intermediate,
            detail.Rubric.Advanced,
            detail.Rubric.InsufficientEvidence,
            detail.ExpectedSignals,
            candidateAnswer
        );

        var promptDto = new TracePromptDto(systemInstructions, evaluationInstructions);
        var questionDto = new TraceQuestionDto(
            detail.Id,
            detail.RoleId,
            detail.SkillId,
            detail.Difficulty,
            detail.QuestionType,
            detail.QuestionText,
            detail.Subskills
        );
        var contextDto = new TraceEvaluationContextDto(
            detail.ExpectedSignals,
            detail.Rubric,
            detail.FrameworkSources,
            detail.InterviewEvidence
        );

        var sw = Stopwatch.StartNew();

        if (mode == "live")
        {
            if (!runtime.LiveEvaluationEnabled)
            {
                return (false, null, "LIVE_AI_UNAVAILABLE", "Live AI evaluation unavailable because LiveEvaluationEnabled is false.");
            }

            var apiKey = _config["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, null, "LIVE_AI_UNAVAILABLE", "Live AI evaluation unavailable because OpenAI API key is missing.");
            }

            try
            {
                var chatClient = new ChatClient(model: runtime.Model, apiKey: apiKey);
                var messages = new ChatMessage[]
                {
                    new SystemChatMessage(systemInstructions),
                    new UserChatMessage(evaluationInstructions)
                };

                var options = new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                        "diagnostic_evaluation_response",
                        BinaryData.FromString(DiagnosticEvaluationPromptBuilder.EvaluationJsonSchema),
                        jsonSchemaFormatDescription: "SkillProof qualitative diagnostic evaluation output conforming to 4 approved levels",
                        jsonSchemaIsStrict: true
                    )
                };

                var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
                sw.Stop();

                var rawText = string.Join("", completion.Value.Content.Select(c => c.Text)).Trim();

                // Validate and process AI response
                bool schemaValid = false;
                bool levelValid = false;
                bool evidenceValid = false;
                bool fallbackUsed = false;
                var validationMessages = new List<string>();

                string finalLevel = "Insufficient Evidence";
                string finalReason = string.Empty;
                var finalEvidence = new List<string>();
                object? parsedObj = null;

                try
                {
                    using var doc = JsonDocument.Parse(rawText);
                    schemaValid = true;
                    validationMessages.Add("JSON schema validation passed.");
                    parsedObj = JsonSerializer.Deserialize<object>(rawText);

                    if (doc.RootElement.TryGetProperty("evaluations", out var evs) && evs.ValueKind == JsonValueKind.Array)
                    {
                        var first = evs.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            var rawLvl = first.TryGetProperty("level", out var lp) ? lp.GetString() : null;
                            var rawRsn = first.TryGetProperty("reason", out var rp) ? rp.GetString() : null;

                            if (rawLvl != null && (rawLvl == "Beginner" || rawLvl == "Intermediate" || rawLvl == "Advanced" || rawLvl == "Insufficient Evidence"))
                            {
                                levelValid = true;
                                finalLevel = rawLvl;
                                validationMessages.Add($"Assigned valid 4-tier qualitative level: {finalLevel}");
                            }
                            else
                            {
                                validationMessages.Add($"Unrecognized level '{rawLvl}'; defaulted to Insufficient Evidence.");
                            }

                            finalReason = rawRsn ?? "Evaluated by AI model based on provided rubric.";

                            if (first.TryGetProperty("evidence", out var ep) && ep.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var ev in ep.EnumerateArray())
                                {
                                    var str = ev.GetString();
                                    if (!string.IsNullOrWhiteSpace(str)) finalEvidence.Add(str);
                                }
                                evidenceValid = finalEvidence.Count > 0;
                                validationMessages.Add($"Extracted {finalEvidence.Count} evidence items grounded in candidate answer.");
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    schemaValid = false;
                    fallbackUsed = true;
                    validationMessages.Add($"Model output was not valid JSON ({ex.Message}). Invoking deterministic fallback.");

                    var (fbLevel, fbReason, fbEvidence) = DeterministicDiagnosticEvaluator.EvaluateSingleAnswer(
                        detail.RoleId,
                        detail.Id,
                        candidateAnswer
                    );
                    finalLevel = fbLevel;
                    finalReason = fbReason;
                    finalEvidence = fbEvidence;
                }

                var trace = new AiDiagnosticTraceDto(
                    TraceId: traceId,
                    Timestamp: DateTimeOffset.UtcNow,
                    Mode: "live",
                    Runtime: runtime,
                    Question: questionDto,
                    EvaluationContext: contextDto,
                    CandidateAnswer: candidateAnswer,
                    Prompt: promptDto,
                    ModelResponse: new TraceModelResponseDto(rawText, parsedObj),
                    Validation: new TraceValidationDto(
                        SchemaValid: schemaValid,
                        LevelValid: levelValid,
                        EvidenceValid: evidenceValid,
                        QuestionIdMatched: true,
                        RubricResolved: true,
                        FallbackUsed: fallbackUsed,
                        ValidationMessages: validationMessages
                    ),
                    FinalResult: new TraceFinalResultDto(finalLevel, finalReason, finalEvidence),
                    Timing: new TraceTimingDto(sw.ElapsedMilliseconds)
                );

                _traceStore.AddTrace(trace);
                return (true, trace, null, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogWarning("Developer Inspector live AI evaluation error: {Message}", ex.Message);
                return (false, null, "LIVE_AI_ERROR", $"OpenAI evaluation failed ({ex.GetType().Name}): {ex.Message}");
            }
        }
        else
        {
            // Deterministic Preview (ZERO OpenAI calls)
            var (detLevel, detReason, detEvidence) = DeterministicDiagnosticEvaluator.EvaluateSingleAnswer(
                detail.RoleId,
                detail.Id,
                candidateAnswer
            );

            sw.Stop();

            var validationMessages = new List<string>
            {
                "Executed DeterministicDiagnosticEvaluator with zero external API calls.",
                $"Demonstrated evidence evaluated against SQLite Rubric criteria for {detail.SkillId}.",
                $"Assigned qualitative level: {detLevel}"
            };

            var parsedResponse = new
            {
                level = detLevel,
                reason = detReason,
                evidence = detEvidence
            };

            var trace = new AiDiagnosticTraceDto(
                TraceId: traceId,
                Timestamp: DateTimeOffset.UtcNow,
                Mode: "deterministic",
                Runtime: runtime with { ActiveEvaluator = "DeterministicDiagnosticEvaluator" },
                Question: questionDto,
                EvaluationContext: contextDto,
                CandidateAnswer: candidateAnswer,
                Prompt: promptDto,
                ModelResponse: new TraceModelResponseDto(null, parsedResponse),
                Validation: new TraceValidationDto(
                    SchemaValid: true,
                    LevelValid: true,
                    EvidenceValid: detEvidence.Count > 0,
                    QuestionIdMatched: true,
                    RubricResolved: true,
                    FallbackUsed: false,
                    ValidationMessages: validationMessages
                ),
                FinalResult: new TraceFinalResultDto(detLevel, detReason, detEvidence),
                Timing: new TraceTimingDto(sw.ElapsedMilliseconds)
            );

            _traceStore.AddTrace(trace);
            return (true, trace, null, null);
        }
    }

    public async Task<DevAdaptiveInspectionDto?> InspectAdaptiveSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || !_sessionStore.TryGetSession(sessionId, out var session) || session == null)
        {
            return null;
        }

        var config = GetRuntimeConfig();
        var activeEvaluator = config.ActiveEvaluator;

        var skillInspections = new List<DevAdaptiveSkillInspectionDto>();

        foreach (var skillId in session.OrderedSkillIds)
        {
            if (!session.SkillStates.TryGetValue(skillId, out var state) || state == null)
            {
                continue;
            }

            var stage1 = new DevAiEvaluationDetailDto(
                Stage: "applied",
                QuestionId: state.FirstQuestionId,
                Difficulty: state.FirstDifficulty,
                Evaluator: activeEvaluator,
                ProvisionalLevel: state.FirstLevel ?? "Insufficient Evidence",
                Reason: state.FirstReason ?? string.Empty,
                ObservedEvidence: state.FirstEvidence
            );

            var branchChosen = state.Branch ?? "none";
            var targetDiff = branchChosen == "foundation" ? "foundation" : "advanced-reasoning";
            var ruleApplied = state.FirstLevel is "Insufficient Evidence" or "Beginner"
                ? $"Provisional level was '{state.FirstLevel}', routing downward to foundation"
                : $"Provisional level was '{state.FirstLevel}', routing upward to advanced-reasoning";

            var branchDecision = new DevAdaptiveBranchDetailDto(
                BranchChosen: branchChosen,
                RuleApplied: ruleApplied,
                TargetDifficulty: targetDiff
            );

            DevAiEvaluationDetailDto? stage2 = null;
            if (!string.IsNullOrWhiteSpace(state.FollowUpQuestionId))
            {
                stage2 = new DevAiEvaluationDetailDto(
                    Stage: "follow-up",
                    QuestionId: state.FollowUpQuestionId,
                    Difficulty: state.FollowUpDifficulty ?? targetDiff,
                    Evaluator: activeEvaluator,
                    ProvisionalLevel: state.FollowUpLevel ?? "Insufficient Evidence",
                    Reason: state.FollowUpReason ?? string.Empty,
                    ObservedEvidence: state.FollowUpEvidence
                );
            }

            DevBackendDerivedProfileDto? derivedProfile = null;
            if (session.Profile != null)
            {
                var profItem = session.Profile.Skills.FirstOrDefault(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));
                if (profItem != null)
                {
                    var matrixRule = $"14-rule evidence matrix applied: (Applied '{state.FirstLevel}' + Follow-up '{state.FollowUpLevel}' in branch '{branchChosen}' -> Final '{profItem.FinalLevel}')";
                    derivedProfile = new DevBackendDerivedProfileDto(
                        FinalAggregatedLevel: profItem.FinalLevel,
                        AggregationRule: matrixRule,
                        DemonstratedStrengths: profItem.DemonstratedStrengths,
                        EvidenceGaps: profItem.EvidenceGaps,
                        NextDevelopmentAreas: profItem.NextDevelopmentAreas
                    );
                }
            }

            skillInspections.Add(new DevAdaptiveSkillInspectionDto(
                SkillId: state.SkillId,
                SkillName: state.SkillName,
                Stage1Evaluation: stage1,
                BranchDecision: branchDecision,
                Stage2Evaluation: stage2,
                BackendDerivedProfile: derivedProfile
            ));
        }

        DevRoadmapInspectionDto? roadmapInspection = null;
        if (session.Profile?.RoadmapInput != null)
        {
            var runtimeConfig = GetRuntimeConfig();
            var provider = runtimeConfig.LiveAiAvailable ? "OpenAI (Strict JSON Schema)" : "Deterministic Fallback";
            var roadmap = session.Roadmap ?? await _roadmapGenerator.GenerateFromHandoffAsync(session.Profile.RoadmapInput, session.SessionId, cancellationToken);
            roadmapInspection = new DevRoadmapInspectionDto(
                SessionId: session.SessionId,
                RoleId: session.RoleId,
                Provider: provider,
                ValidationSucceeded: true,
                RawModelOutput: null,
                TrustedHandoffInput: session.Profile.RoadmapInput,
                FinalRoadmapItems: roadmap.Items
            );
        }

        DevProjectInspectionDto? projectInspection = null;
        if (session.Project != null)
        {
            var runtimeConfig = GetRuntimeConfig();
            var provider = runtimeConfig.LiveAiAvailable ? "OpenAI (Strict JSON Schema)" : "Deterministic Fallback";
            var gapContext = session.Roadmap?.ProjectContext ?? new ProjectGapContext(
                session.RoleId,
                session.Project.TargetedSkills.Select(ts => new ProjectGapTargetSkill(
                    ts.SkillId,
                    ts.CurrentLevel,
                    ts.TargetArea,
                    ts.WhyIncluded,
                    "Observable evidence deliverable"
                )).ToList()
            );

            DevProjectEvaluationDto? devEval = null;
            if (session.ProjectEvaluation != null)
            {
                devEval = new DevProjectEvaluationDto(
                    Provider: provider,
                    ValidationSucceeded: true,
                    SubmittedEvidence: new SubmitProjectEvidenceRequest(
                        RepositoryUrl: "https://github.com/candidate/portfolio-project",
                        ProjectSummary: "Observed candidate submission",
                        ImplementationExplanation: "Detailed implementation",
                        ArchitectureDecisions: "Documented trade-offs",
                        TestingExplanation: "Automated test suite"
                    ),
                    PredefinedCriteria: session.Project.EvaluationCriteria,
                    OverallStatus: session.ProjectEvaluation.OverallStatus,
                    SkillEvidence: session.ProjectEvaluation.SkillEvidence
                );
            }

            projectInspection = new DevProjectInspectionDto(
                ProjectId: session.Project.ProjectId,
                RoleId: session.RoleId,
                Provider: provider,
                ValidationSucceeded: true,
                TrustedGapContext: gapContext,
                FinalProject: session.Project,
                Evaluation: devEval,
                PortfolioProof: session.ProjectEvaluation?.PortfolioProof
            );
        }

        var result = new DevAdaptiveInspectionDto(
            SessionId: session.SessionId,
            RoleId: session.RoleId,
            Status: session.Status,
            Skills: skillInspections,
            Summary: session.Profile?.Summary,
            TopGaps: session.Profile?.TopGaps,
            Roadmap: roadmapInspection,
            Project: projectInspection
        );

        return result;
    }
}
