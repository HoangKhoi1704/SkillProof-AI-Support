namespace SkillProof.Api.Models;

public record RoadmapInputItem(
    string Skill,
    int? Priority = null,
    string? LearningGoal = null
);

public record RecommendProjectRequest(
    string RoleId,
    List<string>? TopGaps = null,
    List<RoadmapInputItem>? Roadmap = null,
    string? SessionId = null
);

public record ProjectRequirementDto(
    string Requirement,
    string TargetsSkill,
    string? Deliverable = null,
    string? SkillId = null
);

public record ProjectRecommendationResponse(
    string Title,
    string Description,
    string Reason,
    List<ProjectRequirementDto> Requirements,
    List<string>? ExpectedDeliverables = null
);

public record TargetedSkillDto(
    string SkillId,
    string CurrentLevel,
    string TargetArea,
    string WhyIncluded
);

public record GapBasedProjectDto(
    string ProjectId,
    string RoleId,
    string Title,
    string Scenario,
    string Objective,
    List<TargetedSkillDto> TargetedSkills,
    List<ProjectRequirementDto> Requirements,
    List<string> Deliverables,
    List<string> EvidenceRequirements,
    List<string> EvaluationCriteria,
    string PortfolioOutcome
);

public record SubmitProjectEvidenceRequest(
    string? RepositoryUrl,
    string ProjectSummary,
    string ImplementationExplanation,
    string ArchitectureDecisions,
    string TestingExplanation,
    List<string>? EvidenceExcerpts = null
);

public record SkillEvidenceResultDto(
    string SkillId,
    string EvidenceStatus, // "Demonstrated" | "Partially Demonstrated" | "Insufficient Evidence"
    List<string> Evidence,
    List<string> MissingEvidence
);

public record RequirementEvaluationResultDto(
    string Requirement,
    string TargetsSkill,
    string Status, // "Demonstrated" | "Partially Demonstrated" | "Insufficient Evidence"
    string EvaluationNotes
);

public record PortfolioProofDto(
    string ProjectTitle,
    string Summary,
    List<string> DemonstratedSkills,
    List<string> PortfolioBullets,
    List<string> CvBullets,
    List<string> EvidenceNotes
);

public record ProjectEvaluationDto(
    string ProjectId,
    string OverallStatus, // "Demonstrated" | "Partially Demonstrated" | "Insufficient Evidence"
    List<SkillEvidenceResultDto> SkillEvidence,
    List<RequirementEvaluationResultDto> RequirementResults,
    List<string> DemonstratedEvidence,
    List<string> MissingEvidence,
    List<string> ImprovementSuggestions,
    PortfolioProofDto? PortfolioProof = null
);

public record DevProjectInspectionDto(
    string ProjectId,
    string RoleId,
    string Provider,
    bool ValidationSucceeded,
    ProjectGapContext TrustedGapContext,
    GapBasedProjectDto FinalProject,
    DevProjectEvaluationDto? Evaluation = null,
    PortfolioProofDto? PortfolioProof = null
);

public record DevProjectEvaluationDto(
    string Provider,
    bool ValidationSucceeded,
    SubmitProjectEvidenceRequest SubmittedEvidence,
    List<string> PredefinedCriteria,
    string OverallStatus,
    List<SkillEvidenceResultDto> SkillEvidence
);
