using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IDeploymentEvidenceVerifier
{
    Task<DeploymentEvidenceResult> VerifyDeploymentAsync(
        string deployedUrl,
        string roleId,
        CancellationToken cancellationToken = default
    );
}
