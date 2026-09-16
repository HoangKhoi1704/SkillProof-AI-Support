using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IRoadmapGenerator
{
    Task<RoadmapResponse> GenerateAsync(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken = default
    );

    Task<RoadmapResponse> GenerateFromHandoffAsync(
        RoadmapHandoffContract handoff,
        string? sessionId = null,
        CancellationToken cancellationToken = default
    );
}
