using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IDataAnalystEvidenceVerifier
{
    Task<DataAnalystEvidenceResult> VerifyDataAnalystEvidenceAsync(
        string? notebookUrl,
        string? datasetUrl,
        string? dashboardUrl,
        string? notes,
        CancellationToken cancellationToken = default
    );
}
