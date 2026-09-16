using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IProjectRecommender
{
    Task<ProjectRecommendationResponse> RecommendAsync(
        RecommendProjectRequest request,
        CancellationToken cancellationToken = default
    );

    Task<GapBasedProjectDto> RecommendGapBasedAsync(
        ProjectGapContext context,
        CancellationToken cancellationToken = default
    );
}
