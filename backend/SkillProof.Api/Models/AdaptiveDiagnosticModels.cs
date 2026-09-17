using System;
using System.Collections.Generic;

namespace SkillProof.Api.Models;

public record AdaptiveSessionRequest(
    string RoleId,
    List<string>? SelectedSkillIds = null,
    string? PrimaryLanguageId = null,
    bool? IncludeMandatoryFundamentals = null
);

public record AdaptiveAnswerRequest(
    string QuestionId,
    string Answer
);

public record AdaptivePublicQuestionDto(
    string Id,
    string SkillId,
    string SkillName,
    string Difficulty,
    string QuestionType,
    string QuestionText
);

public record AdaptiveProgressDto(
    int CurrentSkillIndex,
    int TotalSkills,
    string CurrentSkillId,
    string CurrentSkillName,
    string Stage,
    int CompletedSkills,
    int TotalAnswered
);

public record AdaptiveSkillResultDto(
    string SkillId,
    string SkillName,
    string FinalLevel,
    string Reason,
    List<string> Evidence,
    int QuestionsAnswered,
    string Branch,
    string AppliedDifficulty,
    string AppliedLevel,
    string FollowUpDifficulty,
    string FollowUpLevel
);

public record ProfileSummaryDto(
    int IntermediateCount,
    int BeginnerCount,
    int AdvancedCount,
    int InsufficientEvidenceCount,
    int TotalSkillsAssessed
);

public record SkillProfileItemDto(
    string SkillId,
    string SkillName,
    string FinalLevel,
    List<string> Evidence,
    List<string> Reasoning,
    List<string> DemonstratedStrengths,
    List<string> EvidenceGaps,
    List<string> NextDevelopmentAreas,
    List<string> QuestionsAnswered
);

public record RoadmapSkillGapInput(
    string SkillId,
    string SkillName,
    string CurrentLevel,
    List<string> DevelopmentAreas,
    List<string> EvidenceGaps
);

public record RoadmapHandoffContract(
    string RoleId,
    List<RoadmapSkillGapInput> SkillGaps
);

public record SkillMatrixItemDto(
    string CanonicalSkillId,
    string SkillName,
    string Category,
    bool IsMandatoryFundamental,
    string OverallStatus, // "Advanced" | "Intermediate" | "Beginner" | "Insufficient Evidence" | "Not Assessed"
    string FundamentalsDimension, // "Demonstrated" | "Emerging" | "Insufficient Evidence" | "Not Assessed"
    string AppliedDimension, // "Demonstrated" | "Emerging" | "Insufficient Evidence" | "Not Assessed"
    string ReasoningDimension, // "Demonstrated" | "Emerging" | "Insufficient Evidence" | "Not Assessed"
    string GapType, // "ASSESSED GAP" | "EVIDENCE GAP" | "ROLE COVERAGE GAP" | "NONE"
    List<string> EvidenceObserved,
    string WhyThisLevel,
    List<string> WhatToImproveNext
);

public record PostAnswerExplanationDto(
    string QuestionId,
    string SkillId,
    string SkillName,
    string EvaluatedLevel,
    List<string> WhatYouCovered,
    List<string> WhatCouldBeStronger,
    string ReferenceExplanation
);

public record CareerReadinessProfile(
    string RoleId,
    string AssessmentType,
    DateTimeOffset CompletedAt,
    ProfileSummaryDto Summary,
    List<SkillProfileItemDto> Skills,
    List<string> TopGaps,
    RoadmapHandoffContract RoadmapInput,
    List<SkillMatrixItemDto>? SkillMatrix = null
);

public record AdaptiveSessionResponse(
    string SessionId,
    string RoleId,
    string Status,
    AdaptivePublicQuestionDto? CurrentQuestion,
    AdaptiveProgressDto? Progress,
    List<AdaptiveSkillResultDto>? Skills,
    List<string>? TopGaps,
    List<string> UnsupportedSkillIds,
    CareerReadinessProfile? Profile = null,
    PostAnswerExplanationDto? LastExplanation = null
);

public class SkillAssessmentState
{
    public string SkillId { get; set; } = string.Empty;
    public string SkillName { get; set; } = string.Empty;

    // Stage 1: Applied
    public string FirstQuestionId { get; set; } = string.Empty;
    public string FirstDifficulty { get; set; } = "applied";
    public string? FirstAnswer { get; set; }
    public string? FirstLevel { get; set; }
    public string? FirstReason { get; set; }
    public List<string> FirstEvidence { get; set; } = new();

    // Adaptive Branch Decision
    public string? Branch { get; set; }

    // Stage 2: Follow-up
    public string? FollowUpQuestionId { get; set; }
    public string? FollowUpDifficulty { get; set; }
    public string? FollowUpAnswer { get; set; }
    public string? FollowUpLevel { get; set; }
    public string? FollowUpReason { get; set; }
    public List<string> FollowUpEvidence { get; set; } = new();

    // Aggregation Result
    public string? FinalLevel { get; set; }
    public string? FinalReason { get; set; }
    public List<string> EvidenceSummary { get; set; } = new();
    public bool IsCompleted { get; set; }
}

public class DiagnosticSessionState
{
    public string SessionId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public List<string> SelectedSkillIds { get; set; } = new();
    public string? PrimaryLanguageId { get; set; }

    public List<string> OrderedSkillIds { get; set; } = new();
    public int CurrentSkillIndex { get; set; }
    public string CurrentStage { get; set; } = "applied"; // "applied" or "follow-up"
    public string CurrentQuestionId { get; set; } = string.Empty;

    public Dictionary<string, SkillAssessmentState> SkillStates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> UnsupportedSkillIds { get; set; } = new();

    public string Status { get; set; } = "in-progress"; // "in-progress" or "completed"
    public CareerReadinessProfile? Profile { get; set; }
    public RoadmapResponse? Roadmap { get; set; }
    public GapBasedProjectDto? Project { get; set; }
    public ProjectEvaluationDto? ProjectEvaluation { get; set; }

    public PostAnswerExplanationDto? LastExplanation { get; set; }
    public bool AwaitingNextQuestion { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
