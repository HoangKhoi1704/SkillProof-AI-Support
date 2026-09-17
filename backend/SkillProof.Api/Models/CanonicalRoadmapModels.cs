namespace SkillProof.Api.Models;

public static class RoadmapNodeStates
{
    public const string Completed = "Completed";
    public const string Current = "Current";
    public const string Available = "Available";
    public const string Locked = "Locked";
    public const string NeedsDevelopment = "NeedsDevelopment";
    public const string NotAssessed = "NotAssessed";
    public const string Optional = "Optional";
}

public record PersonalizedRoadmapNodeDto(
    string CanonicalSkillId,
    string Name,
    string Classification,
    string Requirement,
    string NodeState,
    string GapType,
    string AssessmentState,
    string WhyThisNode,
    string NextAction,
    int DisplayOrder,
    bool IsToolkit,
    bool IsOptional,
    string Category,
    string Importance,
    IReadOnlyList<string> PrerequisiteSkillIds
);

public record PersonalizedRoadmapEdgeDto(
    string From,
    string To,
    string RelationshipType,
    string? Rationale
);

public record RoadmapGraphSummaryDto(
    IReadOnlyList<string> CurrentNodeIds,
    int CompletedCount,
    int NeedsDevelopmentCount,
    int NotAssessedCount,
    int AvailableCount,
    int LockedCount,
    int OptionalCount,
    int TotalNodes
);

public record PersonalizedRoadmapGraphDto(
    string RoleId,
    string RoleTitle,
    string? SessionId,
    IReadOnlyList<PersonalizedRoadmapNodeDto> Nodes,
    IReadOnlyList<PersonalizedRoadmapEdgeDto> Edges,
    RoadmapGraphSummaryDto Summary
);
