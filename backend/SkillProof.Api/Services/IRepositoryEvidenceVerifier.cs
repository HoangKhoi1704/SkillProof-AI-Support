using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IRepositoryEvidenceVerifier
{
    Task<RepositoryEvidenceResult> VerifyRepositoryAsync(
        string repositoryUrl,
        CancellationToken cancellationToken = default
    );
}
