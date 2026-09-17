using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface ILearningResourceService
{
    Task<NodeResourcesResponse> GetNodeResourcesAsync(
        string canonicalSkillId,
        string? roleId = null,
        string? nodeState = null,
        string? gapType = null,
        CancellationToken cancellationToken = default
    );
}
