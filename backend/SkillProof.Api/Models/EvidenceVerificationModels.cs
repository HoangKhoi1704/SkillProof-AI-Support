namespace SkillProof.Api.Models;

public enum EvidenceType
{
    Repository,
    Deployment,
    Notebook,
    Dashboard,
    Dataset,
    CandidateNotes
}

public enum VerificationState
{
    Verified,
    PartiallyVerified,
    Unverified,
    Unavailable,
    Invalid
}

public record RepositoryEvidenceResult(
    bool RepositoryExists,
    string? DefaultBranch,
    List<string> FoundFiles,
    List<string> ManifestFiles,
    List<string> TestFiles,
    string? ReadmeExcerpt,
    List<string> LanguageIndicators,
    List<string> SecurityWarnings,
    VerificationState Status,
    string? ErrorMessage = null
);

public record DeploymentEvidenceResult(
    bool IsReachable,
    int? StatusCode,
    string? ContentType,
    string? PageTitle,
    List<string> ObservedFeatures,
    VerificationState Status,
    string? ErrorMessage = null
);

public record DataAnalystEvidenceResult(
    bool NotebookFound,
    List<string> NotebookImports,
    int CodeCellCount,
    int MarkdownCellCount,
    bool DatasetFound,
    string? DatasetFilename,
    bool DashboardReachable,
    VerificationState Status,
    string? NotesSummary = null,
    string? ErrorMessage = null
);

public record VerificationArtifactItem(
    string EvidenceId,
    EvidenceType Type,
    VerificationState Status,
    string SourceLocation,
    string Summary,
    DateTimeOffset VerifiedAt
);

public record CompositeEvidenceReport(
    RepositoryEvidenceResult? Repository,
    DeploymentEvidenceResult? Deployment,
    DataAnalystEvidenceResult? DataAnalyst,
    List<VerificationArtifactItem> Artifacts,
    List<string> SecurityWarnings
);
