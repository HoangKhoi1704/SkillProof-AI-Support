namespace SkillProof.Api.Models;

public record LearningResourceDto(
    string Id,
    string Title,
    string SourceName,
    string SourceUrl,
    string ResourceType,
    IReadOnlyList<string> CanonicalSkillIds,
    IReadOnlyList<string> RoleIds,
    string Level,
    bool IsOfficial,
    string VerificationStatus,
    string VerifiedAt,
    string? Locator,
    string? RelevanceReason = null
);

public record NodeResourcesResponse(
    string CanonicalSkillId,
    IReadOnlyList<LearningResourceDto> Resources
);

public record CuratedProjectDto(
    string Id,
    string Title,
    string Source,
    string? SourceUrl,
    string? SourceLocator,
    string Provenance,
    IReadOnlyList<string> RoleIds,
    IReadOnlyList<string> CanonicalSkillIds,
    IReadOnlyList<string> RoadmapTargets,
    string ProjectType,
    string Difficulty,
    string EstimatedScope,
    string Description,
    IReadOnlyList<string> Deliverables,
    IReadOnlyList<string> EvidenceRequirements,
    string VerificationStatus,
    string VerifiedAt,
    IReadOnlyList<string> TargetedGaps,
    string WhyRecommended
);

public record CuratedProjectRecommendationsResponse(
    string RoleId,
    IReadOnlyList<CuratedProjectDto> PracticeProjects,
    IReadOnlyList<CuratedProjectDto> PortfolioProjects
);
