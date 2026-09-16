namespace SkillProof.Api.Models;

public record DiagnosticQuestionDto(
    int Id,
    string CareerRoleId,
    string Competency,
    string Type,
    string QuestionText,
    string SourceType,
    string SourceReference
);
