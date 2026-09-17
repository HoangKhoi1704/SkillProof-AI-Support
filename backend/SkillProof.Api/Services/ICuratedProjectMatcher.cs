using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface ICuratedProjectMatcher
{
    Task<(bool Success, CuratedProjectRecommendationsResponse? Recommendations, string? ErrorCode, string? ErrorMessage)> MatchProjectsForSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default
    );

    Task<CuratedProjectDto?> GetProjectByIdAsync(
        string projectId,
        CancellationToken cancellationToken = default
    );

    Task<GapBasedProjectDto?> MapToGapBasedProjectAsync(
        string projectId,
        string sessionId,
        CancellationToken cancellationToken = default
    );
}
