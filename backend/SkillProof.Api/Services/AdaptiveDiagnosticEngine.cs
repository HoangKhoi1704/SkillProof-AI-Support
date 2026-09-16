using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillProof.Api.Services;

public interface IAdaptiveDiagnosticEngine
{
    string DetermineBranch(string appliedLevel);
    string DetermineFinalLevel(string appliedLevel, string followUpLevel, string branch);
    List<string> AggregateEvidence(IReadOnlyList<string>? appliedEvidence, IReadOnlyList<string>? followUpEvidence);
    string FormatFinalReason(string? appliedReason, string? followUpReason);
}

public class AdaptiveDiagnosticEngine : IAdaptiveDiagnosticEngine
{
    public static readonly HashSet<string> ApprovedLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Insufficient Evidence",
        "Beginner",
        "Intermediate",
        "Advanced"
    };

    public string DetermineBranch(string appliedLevel)
    {
        if (string.IsNullOrWhiteSpace(appliedLevel))
        {
            throw new ArgumentException("Applied level cannot be null or empty.", nameof(appliedLevel));
        }

        var normalized = NormalizeLevel(appliedLevel);

        return normalized switch
        {
            "Insufficient Evidence" => "foundation",
            "Beginner" => "foundation",
            "Intermediate" => "advanced-reasoning",
            "Advanced" => "advanced-reasoning",
            _ => throw new ArgumentException($"Unrecognized applied level: '{appliedLevel}'.", nameof(appliedLevel))
        };
    }

    public string DetermineFinalLevel(string appliedLevel, string followUpLevel, string branch)
    {
        if (string.IsNullOrWhiteSpace(appliedLevel))
        {
            throw new ArgumentException("Applied level cannot be null or empty.", nameof(appliedLevel));
        }

        if (string.IsNullOrWhiteSpace(followUpLevel))
        {
            throw new ArgumentException("Follow-up level cannot be null or empty.", nameof(followUpLevel));
        }

        if (string.IsNullOrWhiteSpace(branch))
        {
            throw new ArgumentException("Branch cannot be null or empty.", nameof(branch));
        }

        var normApplied = NormalizeLevel(appliedLevel);
        var normFollowUp = NormalizeLevel(followUpLevel);
        var normBranch = branch.Trim().ToLowerInvariant();

        if (normBranch != "foundation" && normBranch != "advanced-reasoning")
        {
            throw new ArgumentException($"Invalid adaptive branch: '{branch}'. Must be 'foundation' or 'advanced-reasoning'.", nameof(branch));
        }

        // Verify that the branch matches the applied level
        var expectedBranch = DetermineBranch(normApplied);
        if (!string.Equals(normBranch, expectedBranch, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid combination: Applied level '{normApplied}' should branch to '{expectedBranch}', but received '{normBranch}'.");
        }

        // Exact 14-rule evidence matrix implementation
        if (normBranch == "foundation")
        {
            return (normApplied, normFollowUp) switch
            {
                ("Insufficient Evidence", "Insufficient Evidence") => "Insufficient Evidence",
                ("Insufficient Evidence", "Beginner") => "Beginner",
                ("Insufficient Evidence", "Intermediate" or "Advanced") => "Intermediate",

                ("Beginner", "Insufficient Evidence") => "Beginner",
                ("Beginner", "Beginner") => "Beginner",
                ("Beginner", "Intermediate" or "Advanced") => "Intermediate",

                _ => throw new ArgumentException($"Unsupported combination for Foundation branch: Applied '{normApplied}' with Follow-up '{normFollowUp}'.")
            };
        }
        else // advanced-reasoning
        {
            return (normApplied, normFollowUp) switch
            {
                ("Intermediate", "Insufficient Evidence") => "Intermediate",
                ("Intermediate", "Beginner") => "Intermediate",
                ("Intermediate", "Intermediate") => "Intermediate",
                ("Intermediate", "Advanced") => "Advanced",

                ("Advanced", "Insufficient Evidence") => "Intermediate",
                ("Advanced", "Beginner") => "Intermediate",
                ("Advanced", "Intermediate") => "Intermediate",
                ("Advanced", "Advanced") => "Advanced",

                _ => throw new ArgumentException($"Unsupported combination for Advanced branch: Applied '{normApplied}' with Follow-up '{normFollowUp}'.")
            };
        }
    }

    public List<string> AggregateEvidence(IReadOnlyList<string>? appliedEvidence, IReadOnlyList<string>? followUpEvidence)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (appliedEvidence != null)
        {
            foreach (var ev in appliedEvidence)
            {
                if (!string.IsNullOrWhiteSpace(ev) && seen.Add(ev.Trim()))
                {
                    result.Add(ev.Trim());
                }
            }
        }

        if (followUpEvidence != null)
        {
            foreach (var ev in followUpEvidence)
            {
                if (!string.IsNullOrWhiteSpace(ev) && seen.Add(ev.Trim()))
                {
                    result.Add(ev.Trim());
                }
            }
        }

        return result;
    }

    public string FormatFinalReason(string? appliedReason, string? followUpReason)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(appliedReason))
        {
            parts.Add($"Practical Assessment: {appliedReason.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(followUpReason))
        {
            parts.Add($"Follow-up Verification: {followUpReason.Trim()}");
        }

        return parts.Count > 0
            ? string.Join(" ", parts)
            : "Diagnostic assessment complete.";
    }

    private static string NormalizeLevel(string level)
    {
        var trimmed = level.Trim();
        foreach (var approved in ApprovedLevels)
        {
            if (string.Equals(trimmed, approved, StringComparison.OrdinalIgnoreCase))
            {
                return approved;
            }
        }

        throw new ArgumentException($"Level '{level}' is not an approved qualitative level.", nameof(level));
    }
}
