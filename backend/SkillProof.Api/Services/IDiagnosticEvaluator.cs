using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IDiagnosticEvaluator
{
    Task<EvaluationResponse> EvaluateAsync(
        string roleId,
        IReadOnlyList<DiagnosticAnswerSubmission> submissions,
        CancellationToken cancellationToken = default
    );
}
