namespace SkillProof.Api.Models;

public record RubricDefinition(
    string Beginner,
    string Intermediate,
    string Advanced,
    string InsufficientEvidence
);

public record InternalQuestion(
    int Id,
    string CareerRoleId,
    string Competency,
    string Type,
    string QuestionText,
    string SourceType,
    string SourceReference,
    RubricDefinition Rubric
)
{
    public DiagnosticQuestionDto ToPublicDto() =>
        new(Id, CareerRoleId, Competency, Type, QuestionText, SourceType, SourceReference);
}
