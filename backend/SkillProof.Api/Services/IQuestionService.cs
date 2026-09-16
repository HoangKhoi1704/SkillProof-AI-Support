using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IQuestionService
{
    IReadOnlyList<RoleDto> GetRoles();
    bool TryGetQuestions(string roleId, out IReadOnlyList<DiagnosticQuestionDto>? questions);
    InternalQuestion? GetInternalQuestion(int questionId);
    (bool Success, EvaluationResponse? Response, string? ErrorMessage) EvaluateDiagnostic(EvaluationRequest request);
    Task<(bool Success, EvaluationResponse? Response, string? ErrorMessage)> EvaluateDiagnosticAsync(EvaluationRequest request, CancellationToken cancellationToken = default);
}
