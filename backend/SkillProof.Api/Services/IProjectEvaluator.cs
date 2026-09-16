using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IProjectEvaluator
{
    Task<ProjectEvaluationDto> EvaluateAsync(
        GapBasedProjectDto project,
        SubmitProjectEvidenceRequest request,
        CancellationToken cancellationToken = default
    );
}
