namespace SkillProof.Api.Models;

public record RoadmapSkillInput(
    string Name,
    string Level,
    string? Reason = null,
    List<string>? Evidence = null
);

public record GenerateRoadmapRequest(
    string RoleId,
    List<string>? TopGaps = null,
    List<RoadmapSkillInput>? Skills = null,
    string? SessionId = null,
    RoadmapHandoffContract? HandoffContract = null
);

public record RoadmapItemDto(
    string Skill,
    int Priority,
    string LearningGoal,
    string PracticeTask,
    string? SkillId = null,
    string? CurrentLevel = null,
    string? GapType = null,
    string? TargetArea = null,
    string? WhyThisMatters = null,
    List<string>? LearningActions = null,
    string? EvidenceTarget = null,
    List<string>? CompletionCriteria = null
);

public record RoadmapResponse(
    string RoleId,
    List<RoadmapItemDto> Items,
    DateTimeOffset? GeneratedAt = null,
    string? SessionId = null,
    ProjectGapContext? ProjectContext = null
);

public record ProjectGapContext(
    string RoleId,
    List<ProjectGapTargetSkill> TargetSkills
);

public record ProjectGapTargetSkill(
    string SkillId,
    string CurrentLevel,
    string TargetArea,
    string PracticeTask,
    string EvidenceTarget
);

public record DevRoadmapInspectionDto(
    string SessionId,
    string RoleId,
    string Provider,
    bool ValidationSucceeded,
    string? RawModelOutput,
    RoadmapHandoffContract TrustedHandoffInput,
    List<RoadmapItemDto> FinalRoadmapItems
);
