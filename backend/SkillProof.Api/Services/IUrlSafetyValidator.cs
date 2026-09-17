namespace SkillProof.Api.Services;

public record UrlSafetyCheckResult(
    bool IsSafe,
    string? NormalizedUrl,
    string? FailureReason
);

public interface IUrlSafetyValidator
{
    UrlSafetyCheckResult ValidateUrl(string? rawUrl, bool allowHttpInDev = false);
    Task<UrlSafetyCheckResult> ValidateUrlAsync(string? rawUrl, bool allowHttpInDev = false, CancellationToken ct = default);
}
