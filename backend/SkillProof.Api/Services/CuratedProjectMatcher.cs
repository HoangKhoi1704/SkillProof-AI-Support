using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class CuratedProjectMatcher : ICuratedProjectMatcher
{
    private readonly CatalogDbContext _db;
    private readonly ICanonicalRoadmapResolver _roadmapResolver;
    private readonly IAdaptiveSessionStore _sessionStore;

    public CuratedProjectMatcher(
        CatalogDbContext db,
        ICanonicalRoadmapResolver roadmapResolver,
        IAdaptiveSessionStore sessionStore)
    {
        _db = db;
        _roadmapResolver = roadmapResolver;
        _sessionStore = sessionStore;
    }

    public async Task<(bool Success, CuratedProjectRecommendationsResponse? Recommendations, string? ErrorCode, string? ErrorMessage)> MatchProjectsForSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return (false, null, "INVALID_SESSION_ID", "Session ID cannot be empty.");
        }

        var (success, graph, errorCode, errorMessage) = await _roadmapResolver.ResolveRoadmapAsync(sessionId, cancellationToken);
        if (!success || graph == null)
        {
            return (false, null, errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Could not resolve roadmap for session.");
        }

        var roleId = graph.RoleId;
        var allProjects = await _db.CuratedProjects.ToListAsync(cancellationToken);

        var nodeMap = graph.Nodes.ToDictionary(n => n.CanonicalSkillId, StringComparer.OrdinalIgnoreCase);

        var practiceCandidates = new List<(CuratedProject project, CuratedProjectDto dto, int score)>();
        var portfolioCandidates = new List<(CuratedProject project, CuratedProjectDto dto, int score)>();

        foreach (var p in allProjects)
        {
            List<string> roleIds;
            try
            {
                roleIds = JsonSerializer.Deserialize<List<string>>(p.RoleIdsJson) ?? new List<string>();
            }
            catch
            {
                roleIds = new List<string>();
            }

            if (!roleIds.Any(r => string.Equals(r, roleId, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            List<string> skillIds;
            List<string> targets;
            List<string> deliverables;
            List<string> evidenceReqs;
            try
            {
                skillIds = JsonSerializer.Deserialize<List<string>>(p.CanonicalSkillIdsJson) ?? new List<string>();
                targets = JsonSerializer.Deserialize<List<string>>(p.RoadmapTargetsJson) ?? new List<string>();
                deliverables = JsonSerializer.Deserialize<List<string>>(p.DeliverablesJson) ?? new List<string>();
                evidenceReqs = JsonSerializer.Deserialize<List<string>>(p.EvidenceRequirementsJson) ?? new List<string>();
            }
            catch
            {
                continue;
            }

            int score = 0;
            var targetedGaps = new List<string>();

            foreach (var skId in skillIds)
            {
                if (nodeMap.TryGetValue(skId, out var node))
                {
                    if (string.Equals(node.NodeState, "Current", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 100;
                        targetedGaps.Add($"{skId} (Current Priority)");
                    }
                    else if (string.Equals(node.NodeState, "NeedsDevelopment", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 50;
                        targetedGaps.Add($"{skId} (Needs Development)");
                    }
                    else if (string.Equals(node.NodeState, "Available", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 20;
                        targetedGaps.Add($"{skId} (Available Next)");
                    }
                    else if (string.Equals(node.NodeState, "Completed", StringComparison.OrdinalIgnoreCase))
                    {
                        score -= 20; // Deprioritize completed competencies
                    }
                    else if (string.Equals(node.NodeState, "Optional", StringComparison.OrdinalIgnoreCase))
                    {
                        score -= 10; // Deprioritize elective specializations
                    }

                    if (string.Equals(node.GapType, "ASSESSED_CORE_GAP", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 40;
                    }
                    else if (string.Equals(node.GapType, "EVIDENCE_GAP", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 30;
                    }
                    else if (string.Equals(node.GapType, "ROLE_COVERAGE_GAP", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 15;
                    }
                }
            }

            var whyRecommended = targetedGaps.Count > 0
                ? $"Directly targets diagnosed focus areas: {string.Join(", ", targetedGaps)}."
                : "Provides foundational hands-on practical competency reinforcement.";

            var dto = new CuratedProjectDto(
                p.Id,
                p.Title,
                p.Source,
                p.SourceUrl,
                p.SourceLocator,
                p.Provenance,
                roleIds,
                skillIds,
                targets,
                p.ProjectType,
                p.Difficulty,
                p.EstimatedScope,
                p.Description,
                deliverables,
                evidenceReqs,
                p.VerificationStatus,
                p.VerifiedAt,
                targetedGaps,
                whyRecommended
            );

            if (string.Equals(p.ProjectType, "practice", StringComparison.OrdinalIgnoreCase))
            {
                practiceCandidates.Add((p, dto, score));
            }
            else
            {
                portfolioCandidates.Add((p, dto, score));
            }
        }

        var practiceSorted = practiceCandidates
            .OrderByDescending(c => c.score)
            .ThenByDescending(c => c.dto.TargetedGaps.Count)
            .ThenBy(c => c.dto.Title, StringComparer.Ordinal)
            .Select(c => c.dto)
            .Take(3)
            .ToList();

        var portfolioSorted = portfolioCandidates
            .OrderByDescending(c => c.score)
            .ThenByDescending(c => c.dto.TargetedGaps.Count)
            .ThenBy(c => c.dto.Title, StringComparer.Ordinal)
            .Select(c => c.dto)
            .Take(2)
            .ToList();

        var response = new CuratedProjectRecommendationsResponse(
            RoleId: roleId,
            PracticeProjects: practiceSorted,
            PortfolioProjects: portfolioSorted
        );

        return (true, response, null, null);
    }

    public async Task<CuratedProjectDto?> GetProjectByIdAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return null;

        var p = await _db.CuratedProjects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
        if (p == null) return null;

        List<string> roleIds = JsonSerializer.Deserialize<List<string>>(p.RoleIdsJson) ?? new List<string>();
        List<string> skillIds = JsonSerializer.Deserialize<List<string>>(p.CanonicalSkillIdsJson) ?? new List<string>();
        List<string> targets = JsonSerializer.Deserialize<List<string>>(p.RoadmapTargetsJson) ?? new List<string>();
        List<string> deliverables = JsonSerializer.Deserialize<List<string>>(p.DeliverablesJson) ?? new List<string>();
        List<string> evidenceReqs = JsonSerializer.Deserialize<List<string>>(p.EvidenceRequirementsJson) ?? new List<string>();

        return new CuratedProjectDto(
            p.Id,
            p.Title,
            p.Source,
            p.SourceUrl,
            p.SourceLocator,
            p.Provenance,
            roleIds,
            skillIds,
            targets,
            p.ProjectType,
            p.Difficulty,
            p.EstimatedScope,
            p.Description,
            deliverables,
            evidenceReqs,
            p.VerificationStatus,
            p.VerifiedAt,
            new List<string>(),
            "Curated benchmark project from authorized catalog."
        );
    }

    public async Task<GapBasedProjectDto?> MapToGapBasedProjectAsync(
        string projectId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var projectDto = await GetProjectByIdAsync(projectId, cancellationToken);
        if (projectDto == null) return null;

        var targetedSkills = projectDto.CanonicalSkillIds.Select(s => new TargetedSkillDto(
            SkillId: s,
            CurrentLevel: projectDto.Difficulty,
            TargetArea: s,
            WhyIncluded: $"Curated project competency targeting {s}."
        )).ToList();

        var requirements = projectDto.Deliverables.Select((d, idx) => new ProjectRequirementDto(
            Requirement: d,
            TargetsSkill: projectDto.CanonicalSkillIds.ElementAtOrDefault(idx % projectDto.CanonicalSkillIds.Count) ?? projectDto.CanonicalSkillIds.First(),
            Deliverable: d,
            SkillId: projectDto.CanonicalSkillIds.ElementAtOrDefault(idx % projectDto.CanonicalSkillIds.Count) ?? projectDto.CanonicalSkillIds.First()
        )).ToList();

        var gapBased = new GapBasedProjectDto(
            ProjectId: projectDto.Id,
            RoleId: projectDto.RoleIds.FirstOrDefault() ?? "backend-developer",
            Title: projectDto.Title,
            Scenario: projectDto.Description,
            Objective: projectDto.Description,
            TargetedSkills: targetedSkills,
            Requirements: requirements,
            Deliverables: projectDto.Deliverables.ToList(),
            EvidenceRequirements: projectDto.EvidenceRequirements.ToList(),
            EvaluationCriteria: projectDto.EvidenceRequirements.ToList(),
            PortfolioOutcome: $"Verified portfolio artifact for {projectDto.Title}"
        );

        if (_sessionStore.TryGetSession(sessionId, out var sessionState) && sessionState != null)
        {
            sessionState.Project = gapBased;
            _sessionStore.UpdateSession(sessionState);
        }

        return gapBased;
    }
}
