using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface ICanonicalRoadmapResolver
{
    Task<(bool Success, PersonalizedRoadmapGraphDto? Graph, string? ErrorCode, string? ErrorMessage)> ResolveRoadmapAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task<PersonalizedRoadmapGraphDto> ResolveRoadmapForProfileAsync(
        string roleId,
        CareerReadinessProfile profile,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );
}
