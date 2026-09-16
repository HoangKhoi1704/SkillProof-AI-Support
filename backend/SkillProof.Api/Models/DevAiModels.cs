using System.Text.Json.Serialization;

namespace SkillProof.Api.Models;

public record AiRuntimeConfigDto(
    string Provider,
    string Model,
    bool LiveEvaluationEnabled,
    bool CredentialsConfigured,
    bool LiveAiAvailable,
    string ActiveEvaluator,
    string Environment,
    string StatusMessage
);

public record DevQuestionSummaryDto(
    string Id,
    string RoleId,
    string SkillId,
    string Difficulty,
    string QuestionType,
    string QuestionText,
    string VerificationStatus
);

public record DevRubricDto(
    string InsufficientEvidence,
    string Beginner,
    string Intermediate,
    string Advanced
);

public record DevSourceRefDto(
    string SourceId,
    string Title,
    string Publisher,
    string SourceType,
    string Url
);

public record DevInterviewEvidenceDto(
    string SourceId,
    string EvidenceType,
    string EvidenceStrength,
    string Notes,
    List<string> SupportedTopics
);

public record DevQuestionDetailDto(
    string Id,
    string RoleId,
    string SkillId,
    string Difficulty,
    string QuestionType,
    string QuestionText,
    string Provenance,
    string VerificationStatus,
    List<string> Subskills,
    List<string> ExpectedSignals,
    DevRubricDto Rubric,
    List<DevSourceRefDto> FrameworkSources,
    List<DevInterviewEvidenceDto> InterviewEvidence
);

public record DevEvaluateRequest(
    string QuestionId,
    string CandidateAnswer,
    string Mode = "deterministic"
);

public record TraceQuestionDto(
    string QuestionId,
    string RoleId,
    string SkillId,
    string Difficulty,
    string QuestionType,
    string QuestionText,
    List<string> Subskills
);

public record TraceEvaluationContextDto(
    List<string> ExpectedSignals,
    DevRubricDto Rubric,
    List<DevSourceRefDto> FrameworkSources,
    List<DevInterviewEvidenceDto> InterviewEvidence
);

public record TracePromptDto(
    string SystemInstructions,
    string EvaluationInstructions
);

public record TraceModelResponseDto(
    string? RawResponse,
    object? ParsedResponse
);

public record TraceValidationDto(
    bool SchemaValid,
    bool LevelValid,
    bool EvidenceValid,
    bool QuestionIdMatched,
    bool RubricResolved,
    bool FallbackUsed,
    List<string> ValidationMessages
);

public record TraceFinalResultDto(
    string Level,
    string Reason,
    List<string> Evidence
);

public record TraceTimingDto(
    long DurationMs
);

public record AiDiagnosticTraceDto(
    string TraceId,
    DateTimeOffset Timestamp,
    string Mode,
    AiRuntimeConfigDto Runtime,
    TraceQuestionDto Question,
    TraceEvaluationContextDto EvaluationContext,
    string CandidateAnswer,
    TracePromptDto Prompt,
    TraceModelResponseDto ModelResponse,
    TraceValidationDto Validation,
    TraceFinalResultDto FinalResult,
    TraceTimingDto Timing
);

public record AiDiagnosticTraceSummaryDto(
    string TraceId,
    DateTimeOffset Timestamp,
    string QuestionId,
    string SkillId,
    string Evaluator,
    string Level,
    long DurationMs
);

public record DevAiEvaluationDetailDto(
    string Stage,
    string QuestionId,
    string Difficulty,
    string Evaluator,
    string ProvisionalLevel,
    string Reason,
    List<string> ObservedEvidence
);

public record DevAdaptiveBranchDetailDto(
    string BranchChosen,
    string RuleApplied,
    string TargetDifficulty
);

public record DevBackendDerivedProfileDto(
    string FinalAggregatedLevel,
    string AggregationRule,
    List<string> DemonstratedStrengths,
    List<string> EvidenceGaps,
    List<string> NextDevelopmentAreas
);

public record DevAdaptiveSkillInspectionDto(
    string SkillId,
    string SkillName,
    DevAiEvaluationDetailDto Stage1Evaluation,
    DevAdaptiveBranchDetailDto BranchDecision,
    DevAiEvaluationDetailDto? Stage2Evaluation,
    DevBackendDerivedProfileDto? BackendDerivedProfile
);

public record DevAdaptiveInspectionDto(
    string SessionId,
    string RoleId,
    string Status,
    List<DevAdaptiveSkillInspectionDto> Skills,
    ProfileSummaryDto? Summary,
    List<string>? TopGaps,
    DevRoadmapInspectionDto? Roadmap = null,
    DevProjectInspectionDto? Project = null
);
