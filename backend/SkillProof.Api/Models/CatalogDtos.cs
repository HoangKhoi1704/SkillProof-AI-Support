namespace SkillProof.Api.Models;

public record PublicSubskillDto(
    string Id,
    string Name,
    string? Description
);

public record PublicSkillDto(
    string Id,
    string Name,
    string Category,
    string SkillType,
    string? Description,
    IReadOnlyList<PublicSubskillDto> Subskills
);

public record RoleSkillsResponse(
    string RoleId,
    string RoleTitle,
    string? RoleDescription,
    IReadOnlyList<PublicSkillDto> Core,
    IReadOnlyList<PublicSkillDto> Recommended,
    IReadOnlyList<PublicSkillDto> Optional,
    IReadOnlyList<PublicSkillDto> Languages
);

public record DynamicQuestionSelectionRequest(
    string RoleId,
    List<string> SelectedSkillIds,
    string? PrimaryLanguageId = null
);

public record SelectedQuestionDto(
    string Id,
    string CompetencyId,
    string Competency,
    string Difficulty,
    string QuestionType,
    string QuestionText,
    string Question
);

public record AssessmentCoverageDto(
    List<string> RequestedSkillIds,
    List<string> AssessedSkillIds,
    List<string> UnsupportedSkillIds,
    string? AssessedPrimaryLanguageId = null
);

public record DynamicQuestionSelectionResponse(
    string RoleId,
    List<SelectedQuestionDto> Questions,
    AssessmentCoverageDto AssessmentCoverage
);
