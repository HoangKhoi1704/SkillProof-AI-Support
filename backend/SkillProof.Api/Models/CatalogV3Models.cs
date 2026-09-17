namespace SkillProof.Api.Models;

public record RoleSummaryDto(
    string Id,
    string Title,
    string? Description,
    bool IsPrimaryDemoRole,
    string? RoadmapSourceUrl,
    int DisplayOrder
);

public record CanonicalSkillNodeDto(
    string CanonicalSkillId,
    string DisplayName,
    string Classification,
    string Category,
    string Importance,
    string SourceKind,
    string? RoadmapSource,
    string? RoadmapNodeId,
    string? RoadmapLabel,
    string? Description,
    bool AssessmentEligible,
    bool MandatoryFundamental,
    bool IsToolkitOnly,
    bool IsOptional,
    bool HasQuestionCoverage,
    int DisplayOrder
);

public record RoadmapRelationshipDto(
    int Id,
    string SourceSkillId,
    string TargetSkillId,
    string RelationshipType,
    string? Rationale
);

public record RoleCanonicalFrameworkResponse(
    string RoleId,
    string RoleTitle,
    string? Description,
    bool IsPrimaryDemoRole,
    string? RoadmapSourceUrl,
    IReadOnlyList<CanonicalSkillNodeDto> Nodes,
    IReadOnlyList<RoadmapRelationshipDto> Relationships
);
