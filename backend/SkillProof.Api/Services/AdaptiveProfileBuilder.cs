using System;
using System.Collections.Generic;
using System.Linq;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public interface IAdaptiveProfileBuilder
{
    CareerReadinessProfile BuildProfile(
        DiagnosticSessionState session,
        IReadOnlyDictionary<string, IReadOnlyList<string>> subskillsBySkillId,
        IReadOnlyList<SkillProof.Api.Data.Catalog.RoleRoadmapNode>? roleNodes = null,
        IReadOnlyDictionary<string, string>? canonicalSkillNames = null
    );
}

public class AdaptiveProfileBuilder : IAdaptiveProfileBuilder
{
    public CareerReadinessProfile BuildProfile(
        DiagnosticSessionState session,
        IReadOnlyDictionary<string, IReadOnlyList<string>> subskillsBySkillId,
        IReadOnlyList<SkillProof.Api.Data.Catalog.RoleRoadmapNode>? roleNodes = null,
        IReadOnlyDictionary<string, string>? canonicalSkillNames = null)
    {
        if (session == null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        var skillProfileItems = new List<SkillProfileItemDto>();

        foreach (var skillId in session.OrderedSkillIds)
        {
            if (!session.SkillStates.TryGetValue(skillId, out var state) || state == null)
            {
                continue;
            }

            var finalLevel = state.FinalLevel ?? "Insufficient Evidence";
            var evidence = state.EvidenceSummary ?? new List<string>();

            // Collect reasoning from evaluations
            var reasoning = new List<string>();
            if (!string.IsNullOrWhiteSpace(state.FirstReason))
            {
                reasoning.Add($"Applied: {state.FirstReason.Trim()}");
            }
            if (!string.IsNullOrWhiteSpace(state.FollowUpReason))
            {
                reasoning.Add($"Follow-up: {state.FollowUpReason.Trim()}");
            }

            // Questions answered
            var questionsAnswered = new List<string>();
            if (!string.IsNullOrWhiteSpace(state.FirstQuestionId))
            {
                questionsAnswered.Add(state.FirstQuestionId);
            }
            if (!string.IsNullOrWhiteSpace(state.FollowUpQuestionId))
            {
                questionsAnswered.Add(state.FollowUpQuestionId);
            }

            // Demonstrated Strengths: Strictly grounded in observed evidence
            var demonstratedStrengths = DeriveDemonstratedStrengths(finalLevel, evidence);

            // Evidence Gaps: Explicitly distinguish Development Gap vs Evidence Gap
            var evidenceGaps = DeriveEvidenceGaps(finalLevel, state.SkillName, state.FirstReason);

            // Next Development Areas: Strictly derived from approved catalog subskills
            var catalogSubskills = subskillsBySkillId != null && subskillsBySkillId.TryGetValue(skillId, out var subs)
                ? subs
                : Array.Empty<string>();

            var nextDevelopmentAreas = DeriveNextDevelopmentAreas(finalLevel, state.SkillName, catalogSubskills);

            skillProfileItems.Add(new SkillProfileItemDto(
                SkillId: state.SkillId,
                SkillName: state.SkillName,
                FinalLevel: finalLevel,
                Evidence: evidence,
                Reasoning: reasoning,
                DemonstratedStrengths: demonstratedStrengths,
                EvidenceGaps: evidenceGaps,
                NextDevelopmentAreas: nextDevelopmentAreas,
                QuestionsAnswered: questionsAnswered
            ));
        }

        // Summary Counts (no arbitrary scores or percentages)
        var summary = new ProfileSummaryDto(
            IntermediateCount: skillProfileItems.Count(s => s.FinalLevel.Equals("Intermediate", StringComparison.OrdinalIgnoreCase)),
            BeginnerCount: skillProfileItems.Count(s => s.FinalLevel.Equals("Beginner", StringComparison.OrdinalIgnoreCase)),
            AdvancedCount: skillProfileItems.Count(s => s.FinalLevel.Equals("Advanced", StringComparison.OrdinalIgnoreCase)),
            InsufficientEvidenceCount: skillProfileItems.Count(s => s.FinalLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase)),
            TotalSkillsAssessed: skillProfileItems.Count
        );

        // Compute Top Gaps using existing pipeline
        var evalItems = skillProfileItems
            .Select(s => new SkillEvaluationItem(s.SkillName, s.FinalLevel, string.Join("; ", s.Reasoning), s.Evidence))
            .ToList();

        var topGaps = DeterministicDiagnosticEvaluator.CalculateTopGaps(evalItems);

        // Build Visual Skill Matrix with Qualitative States & Gap Types
        var skillMatrixItems = new List<SkillMatrixItemDto>();

        if (roleNodes != null && roleNodes.Count > 0)
        {
            foreach (var node in roleNodes.OrderBy(n => n.DisplayOrder))
            {
                // Only assessable competencies in role framework
                if (!node.AssessmentEligible && node.IsToolkitOnly)
                {
                    continue;
                }

                var skillName = (canonicalSkillNames != null && canonicalSkillNames.TryGetValue(node.CanonicalSkillId, out var csName))
                    ? csName
                    : node.CanonicalSkillId;

                if (session.SkillStates.TryGetValue(node.CanonicalSkillId, out var state) && state != null && state.IsCompleted)
                {
                    var finalLevel = state.FinalLevel ?? "Insufficient Evidence";
                    var gapType = finalLevel switch
                    {
                        "Advanced" => "NONE",
                        "Insufficient Evidence" => "EVIDENCE GAP",
                        _ => "ASSESSED GAP"
                    };

                    var fundDim = finalLevel is "Intermediate" or "Advanced" ? "Demonstrated" : (finalLevel == "Beginner" ? "Emerging" : "Insufficient Evidence");
                    var appDim = (finalLevel is "Intermediate" or "Advanced" && state.FirstLevel != "Beginner") ? "Demonstrated" : (finalLevel == "Beginner" ? "Emerging" : "Insufficient Evidence");
                    var reasDim = finalLevel == "Advanced" ? "Demonstrated" : (finalLevel == "Intermediate" ? "Emerging" : "Insufficient Evidence");

                    var catalogSubskills = subskillsBySkillId != null && subskillsBySkillId.TryGetValue(node.CanonicalSkillId, out var subs)
                        ? subs
                        : Array.Empty<string>();

                    skillMatrixItems.Add(new SkillMatrixItemDto(
                        CanonicalSkillId: node.CanonicalSkillId,
                        SkillName: skillName,
                        Category: node.Category,
                        IsMandatoryFundamental: node.MandatoryFundamental,
                        OverallStatus: finalLevel,
                        FundamentalsDimension: fundDim,
                        AppliedDimension: appDim,
                        ReasoningDimension: reasDim,
                        GapType: gapType,
                        EvidenceObserved: state.EvidenceSummary ?? new List<string>(),
                        WhyThisLevel: state.FinalReason ?? (state.FirstReason ?? "Assessed via calibrated interview sequence."),
                        WhatToImproveNext: DeriveNextDevelopmentAreas(finalLevel, skillName, catalogSubskills)
                    ));
                }
                else
                {
                    skillMatrixItems.Add(new SkillMatrixItemDto(
                        CanonicalSkillId: node.CanonicalSkillId,
                        SkillName: skillName,
                        Category: node.Category,
                        IsMandatoryFundamental: node.MandatoryFundamental,
                        OverallStatus: "Not Assessed",
                        FundamentalsDimension: "Not Assessed",
                        AppliedDimension: "Not Assessed",
                        ReasoningDimension: "Not Assessed",
                        GapType: "ROLE COVERAGE GAP",
                        EvidenceObserved: new List<string>(),
                        WhyThisLevel: "This role competency was not assessed during this diagnostic session.",
                        WhatToImproveNext: new List<string> { "Take an assessment on this competency or review foundational curriculum." }
                    ));
                }
            }
        }
        else
        {
            foreach (var item in skillProfileItems)
            {
                var gapType = item.FinalLevel switch
                {
                    "Advanced" => "NONE",
                    "Insufficient Evidence" => "EVIDENCE GAP",
                    _ => "ASSESSED GAP"
                };

                skillMatrixItems.Add(new SkillMatrixItemDto(
                    CanonicalSkillId: item.SkillId,
                    SkillName: item.SkillName,
                    Category: "core",
                    IsMandatoryFundamental: true,
                    OverallStatus: item.FinalLevel,
                    FundamentalsDimension: item.FinalLevel is "Intermediate" or "Advanced" ? "Demonstrated" : "Emerging",
                    AppliedDimension: item.FinalLevel is "Intermediate" or "Advanced" ? "Demonstrated" : "Emerging",
                    ReasoningDimension: item.FinalLevel == "Advanced" ? "Demonstrated" : "Emerging",
                    GapType: gapType,
                    EvidenceObserved: item.Evidence,
                    WhyThisLevel: item.Reasoning.FirstOrDefault() ?? "Assessed via calibrated interview sequence.",
                    WhatToImproveNext: item.NextDevelopmentAreas
                ));
            }
        }

        // Roadmap Handoff Contract
        var roadmapInput = new RoadmapHandoffContract(
            RoleId: session.RoleId,
            SkillGaps: skillProfileItems.Select(s => new RoadmapSkillGapInput(
                SkillId: s.SkillId,
                SkillName: s.SkillName,
                CurrentLevel: s.FinalLevel,
                DevelopmentAreas: s.NextDevelopmentAreas,
                EvidenceGaps: s.EvidenceGaps
            )).ToList()
        );

        return new CareerReadinessProfile(
            RoleId: session.RoleId,
            AssessmentType: "adaptive",
            CompletedAt: DateTimeOffset.UtcNow,
            Summary: summary,
            Skills: skillProfileItems,
            TopGaps: topGaps,
            RoadmapInput: roadmapInput,
            SkillMatrix: skillMatrixItems
        );
    }

    private static List<string> DeriveDemonstratedStrengths(string finalLevel, List<string> evidence)
    {
        // Insufficient evidence has no verified strengths (do not fabricate claims)
        if (finalLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase) || evidence == null || evidence.Count == 0)
        {
            return new List<string>();
        }

        // Preserve actual observed evidence as grounded strengths
        return evidence
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> DeriveEvidenceGaps(string finalLevel, string skillName, string? initialReason)
    {
        var gaps = new List<string>();

        if (finalLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase))
        {
            // Category B: Evidence Gap (Assessment did not collect sufficient evidence)
            gaps.Add($"Insufficient Evidence: The assessment did not collect enough technical details to determine current competency in {skillName}.");
            if (!string.IsNullOrWhiteSpace(initialReason))
            {
                gaps.Add($"Assessment observation: {initialReason.Trim()}");
            }
        }
        else if (finalLevel.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
        {
            // Category A: Development Gap
            gaps.Add($"Development Gap: Demonstrated foundational understanding, but lacks verified reasoning for applied architectural patterns, edge cases, and production error handling in {skillName}.");
        }
        else if (finalLevel.Equals("Intermediate", StringComparison.OrdinalIgnoreCase))
        {
            // Category A: Development Gap
            gaps.Add($"Development Gap: Demonstrated sound applied implementation, but has not yet proven advanced reasoning for high-scale optimization, concurrency boundaries, or distributed failure recovery in {skillName}.");
        }
        else if (finalLevel.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
        {
            // Advanced growth area
            gaps.Add($"Next-Level Growth: Strong evidence demonstrated across applied and advanced scenarios. Continue deepening expertise in specialized distributed systems, mission-critical resilience, and emerging ecosystem standards.");
        }

        return gaps;
    }

    private static List<string> DeriveNextDevelopmentAreas(string finalLevel, string skillName, IReadOnlyList<string> catalogSubskills)
    {
        if (catalogSubskills != null && catalogSubskills.Count > 0)
        {
            if (finalLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase) ||
                finalLevel.Equals("Beginner", StringComparison.OrdinalIgnoreCase))
            {
                // Prioritize the primary foundational and applied subskills
                return catalogSubskills.Take(Math.Min(3, catalogSubskills.Count)).ToList();
            }
            else if (finalLevel.Equals("Intermediate", StringComparison.OrdinalIgnoreCase))
            {
                // Prioritize advanced and optimization subskills (latter half)
                var count = Math.Min(3, catalogSubskills.Count);
                return catalogSubskills.Skip(Math.Max(0, catalogSubskills.Count - count)).Take(count).ToList();
            }
            else // Advanced
            {
                // Focus on specialized mastery subskills
                var count = Math.Min(2, catalogSubskills.Count);
                return catalogSubskills.TakeLast(count).ToList();
            }
        }

        // Fallback using clean domain naming if no catalog subskills mapped
        return new List<string>
        {
            $"{skillName} Architecture & Patterns",
            $"{skillName} Production Optimization"
        };
    }
}
