namespace SkillProof.Api.Models;

public record RoadmapSkillInput(
    string Name,
    string Level,
    string? Reason = null,
    List<string>? Evidence = null
);

public record GenerateRoadmapRequest(
    string RoleId,
    List<string> TopGaps,
    List<RoadmapSkillInput> Skills
);

public record RoadmapItemDto(
    string Skill,
    int Priority,
    string LearningGoal,
    string PracticeTask
);

public record RoadmapResponse(
    string RoleId,
    List<RoadmapItemDto> Items
);
