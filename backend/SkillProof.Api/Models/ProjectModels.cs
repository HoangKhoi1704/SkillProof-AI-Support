namespace SkillProof.Api.Models;

public record RoadmapInputItem(
    string Skill,
    int? Priority = null,
    string? LearningGoal = null
);

public record RecommendProjectRequest(
    string RoleId,
    List<string> TopGaps,
    List<RoadmapInputItem>? Roadmap = null
);

public record ProjectRequirementDto(
    string Requirement,
    string TargetsSkill,
    string? Deliverable = null
);

public record ProjectRecommendationResponse(
    string Title,
    string Description,
    string Reason,
    List<ProjectRequirementDto> Requirements,
    List<string>? ExpectedDeliverables = null
);
