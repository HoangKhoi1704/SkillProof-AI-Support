using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class LearningResourceService : ILearningResourceService
{
    private readonly CatalogDbContext _db;

    public LearningResourceService(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<NodeResourcesResponse> GetNodeResourcesAsync(
        string canonicalSkillId,
        string? roleId = null,
        string? nodeState = null,
        string? gapType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(canonicalSkillId))
        {
            return new NodeResourcesResponse(canonicalSkillId ?? string.Empty, Array.Empty<LearningResourceDto>());
        }

        var normalizedSkillId = canonicalSkillId.Trim().ToLowerInvariant();
        var allResources = await _db.LearningResources.ToListAsync(cancellationToken);

        var matching = new List<(LearningResource entity, IReadOnlyList<string> skillIds, IReadOnlyList<string> roleIds, int score)>();

        foreach (var r in allResources)
        {
            List<string> skillIds;
            try
            {
                skillIds = JsonSerializer.Deserialize<List<string>>(r.CanonicalSkillIdsJson) ?? new List<string>();
            }
            catch
            {
                skillIds = new List<string>();
            }

            if (!skillIds.Any(s => string.Equals(s, normalizedSkillId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            List<string> roleIds;
            try
            {
                roleIds = JsonSerializer.Deserialize<List<string>>(r.RoleIdsJson) ?? new List<string>();
            }
            catch
            {
                roleIds = new List<string>();
            }

            // Role filtering if specified
            if (!string.IsNullOrWhiteSpace(roleId) && roleIds.Count > 0)
            {
                if (!roleIds.Any(rId => string.Equals(rId, roleId, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
            }

            // Deterministic score calculation based on nodeState & level & isOfficial
            int score = 0;
            if (r.IsOfficial) score += 10;

            if (string.Equals(nodeState, "Current", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(nodeState, "NeedsDevelopment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gapType, "ASSESSED_CORE_GAP", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(r.Level, "foundation", StringComparison.OrdinalIgnoreCase)) score += 5;
                else if (string.Equals(r.Level, "applied", StringComparison.OrdinalIgnoreCase)) score += 4;
                else score += 2;
            }
            else if (string.Equals(nodeState, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(r.Level, "advanced", StringComparison.OrdinalIgnoreCase)) score += 5;
                else if (string.Equals(r.Level, "applied", StringComparison.OrdinalIgnoreCase)) score += 4;
                else score += 2;
            }
            else
            {
                if (string.Equals(r.Level, "foundation", StringComparison.OrdinalIgnoreCase)) score += 5;
                else if (string.Equals(r.Level, "applied", StringComparison.OrdinalIgnoreCase)) score += 4;
                else score += 3;
            }

            matching.Add((r, skillIds, roleIds, score));
        }

        // Deterministic ordering: score descending, then title ascending
        var selected = matching
            .OrderByDescending(m => m.score)
            .ThenBy(m => m.entity.Title, StringComparer.Ordinal)
            .Take(3)
            .Select(m =>
            {
                var r = m.entity;
                var reason = BuildRelevanceReason(r.Level, r.IsOfficial, nodeState, gapType);
                return new LearningResourceDto(
                    r.Id,
                    r.Title,
                    r.SourceName,
                    r.SourceUrl,
                    r.ResourceType,
                    m.skillIds,
                    m.roleIds,
                    r.Level,
                    r.IsOfficial,
                    r.VerificationStatus,
                    r.VerifiedAt,
                    r.Locator,
                    reason
                );
            })
            .ToList();

        return new NodeResourcesResponse(normalizedSkillId, selected);
    }

    private static string BuildRelevanceReason(string level, bool isOfficial, string? nodeState, string? gapType)
    {
        var officialPrefix = isOfficial ? "Authoritative official documentation" : "Verified industry guide";
        if (string.Equals(nodeState, "NeedsDevelopment", StringComparison.OrdinalIgnoreCase))
        {
            return $"{officialPrefix} recommended for closing verified developmental gaps at the {level} level.";
        }
        if (string.Equals(nodeState, "Current", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(gapType, "ASSESSED_CORE_GAP", StringComparison.OrdinalIgnoreCase))
        {
            return $"{officialPrefix} directly addressing your current milestone focus at the {level} level.";
        }
        if (string.Equals(nodeState, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return $"{officialPrefix} providing deep-dive reference and best practices for completed competency.";
        }
        return $"{officialPrefix} covering core competency concepts at the {level} level.";
    }
}
