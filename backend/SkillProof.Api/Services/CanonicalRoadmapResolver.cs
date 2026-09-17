using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class CanonicalRoadmapResolver : ICanonicalRoadmapResolver
{
    private readonly CatalogDbContext _db;
    private readonly IAdaptiveDiagnosticService _adaptiveService;
    private readonly ILogger<CanonicalRoadmapResolver> _logger;

    public CanonicalRoadmapResolver(
        CatalogDbContext db,
        IAdaptiveDiagnosticService adaptiveService,
        ILogger<CanonicalRoadmapResolver> logger)
    {
        _db = db;
        _adaptiveService = adaptiveService;
        _logger = logger;
    }

    public async Task<(bool Success, PersonalizedRoadmapGraphDto? Graph, string? ErrorCode, string? ErrorMessage)> ResolveRoadmapAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return (false, null, "VALIDATION_ERROR", "Session ID cannot be empty.");
        }

        var (success, profile, errorCode, errorMessage) = await _adaptiveService.GetProfileAsync(sessionId, cancellationToken);
        if (!success || profile == null)
        {
            return (false, null, errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Diagnostic session not found.");
        }

        var graph = await ResolveRoadmapForProfileAsync(profile.RoleId, profile, sessionId, cancellationToken);
        return (true, graph, null, null);
    }

    public async Task<PersonalizedRoadmapGraphDto> ResolveRoadmapForProfileAsync(
        string roleId,
        CareerReadinessProfile profile,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleId = (roleId ?? profile.RoleId).Trim().ToLowerInvariant();

        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id.ToLower() == normalizedRoleId, cancellationToken);

        var roleTitle = role?.Title ?? normalizedRoleId;

        // 1. Load canonical role roadmap nodes ordered by DisplayOrder
        var roleNodes = await _db.RoleRoadmapNodes.AsNoTracking()
            .Where(rrn => rrn.RoleId.ToLower() == normalizedRoleId)
            .Include(rrn => rrn.CanonicalSkill)
            .OrderBy(rrn => rrn.DisplayOrder)
            .ToListAsync(cancellationToken);

        var skillIds = roleNodes.Select(rn => rn.CanonicalSkillId).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 2. Load verified relationships connecting these nodes
        var relationships = await _db.RoadmapRelationships.AsNoTracking()
            .Where(rel => skillIds.Contains(rel.SourceSkillId) && skillIds.Contains(rel.TargetSkillId))
            .OrderBy(rel => rel.Id)
            .ToListAsync(cancellationToken);

        // Sanity validation: no self-edges, no duplicates
        var edges = new List<PersonalizedRoadmapEdgeDto>();
        var seenEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rel in relationships)
        {
            if (rel.SourceSkillId.Equals(rel.TargetSkillId, StringComparison.OrdinalIgnoreCase))
            {
                continue; // Skip self edge if any
            }

            var edgeKey = $"{rel.SourceSkillId}->{rel.TargetSkillId}:{rel.RelationshipType}";
            if (seenEdges.Add(edgeKey))
            {
                edges.Add(new PersonalizedRoadmapEdgeDto(
                    From: rel.SourceSkillId,
                    To: rel.TargetSkillId,
                    RelationshipType: rel.RelationshipType,
                    Rationale: rel.Rationale
                ));
            }
        }

        // Map prerequisite incoming edges: targetSkillId -> list of sourceSkillIds
        var prerequisitesByTarget = edges
            .Where(e => e.RelationshipType.Equals("prerequisite", StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.To, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.From).ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        // Map node by skill id for display names
        var nodesBySkillId = roleNodes.ToDictionary(n => n.CanonicalSkillId, StringComparer.OrdinalIgnoreCase);

        // 3. Index assessment results from SkillMatrix (or Skills fallback)
        var matrixBySkillId = new Dictionary<string, SkillMatrixItemDto>(StringComparer.OrdinalIgnoreCase);
        if (profile.SkillMatrix != null)
        {
            foreach (var item in profile.SkillMatrix)
            {
                matrixBySkillId[item.CanonicalSkillId] = item;
            }
        }

        var skillProfilesById = profile.Skills.ToDictionary(s => s.SkillId, StringComparer.OrdinalIgnoreCase);

        // 4. Initial evaluation pass for each node based on assessment evidence
        var nodeIntermediates = new List<NodeEvaluationIntermediate>();

        foreach (var rn in roleNodes)
        {
            var skillId = rn.CanonicalSkillId;
            var skillName = rn.CanonicalSkill.DisplayName;

            string assessmentState = "Not Assessed";
            string gapType = "NONE";
            string preliminaryState;
            string whyThisNode;
            string nextAction;

            if (matrixBySkillId.TryGetValue(skillId, out var matrixItem))
            {
                assessmentState = matrixItem.OverallStatus;
                gapType = matrixItem.GapType;

                switch (assessmentState)
                {
                    case "Advanced":
                        preliminaryState = RoadmapNodeStates.Completed;
                        gapType = "NONE";
                        whyThisNode = $"Assessment verified advanced reasoning and practical mastery in {skillName}.";
                        nextAction = "Apply advanced concepts in production architectures";
                        break;

                    case "Intermediate":
                        if (gapType == "ASSESSED GAP" || matrixItem.WhatToImproveNext.Count > 0)
                        {
                            preliminaryState = RoadmapNodeStates.NeedsDevelopment;
                            whyThisNode = $"Assessment demonstrated practical {skillName} understanding, but advanced reasoning still needs development.";
                            nextAction = "Deepen architectural reasoning and edge-case handling";
                        }
                        else
                        {
                            preliminaryState = RoadmapNodeStates.Completed;
                            gapType = "NONE";
                            whyThisNode = $"Assessment verified solid practical competence in {skillName}.";
                            nextAction = "Continue to subsequent competencies";
                        }
                        break;

                    case "Beginner":
                        preliminaryState = RoadmapNodeStates.NeedsDevelopment;
                        gapType = "ASSESSED GAP";
                        whyThisNode = $"Practical implementation experience in {skillName} requires focused development.";
                        nextAction = "Review core principles and practice implementation";
                        break;

                    case "Insufficient Evidence":
                        preliminaryState = RoadmapNodeStates.NeedsDevelopment;
                        gapType = "EVIDENCE GAP";
                        whyThisNode = $"Assessment interview provided insufficient evidence to verify {skillName}.";
                        nextAction = "Validate knowledge and demonstrate practical evidence";
                        break;

                    default: // "Not Assessed"
                        assessmentState = "Not Assessed";
                        if (rn.IsOptional)
                        {
                            preliminaryState = RoadmapNodeStates.Optional;
                            gapType = "NONE";
                            whyThisNode = $"Optional elective competency for {roleTitle}.";
                            nextAction = "Elective study if relevant to specializations";
                        }
                        else if (rn.IsToolkitOnly)
                        {
                            preliminaryState = RoadmapNodeStates.Available;
                            gapType = "NONE";
                            whyThisNode = $"Industry developer tooling supporting your professional workflow.";
                            nextAction = "Familiarize with tooling and collaboration standards";
                        }
                        else
                        {
                            preliminaryState = RoadmapNodeStates.NotAssessed;
                            gapType = "ROLE COVERAGE GAP";
                            whyThisNode = $"Not assessed yet. This is a role competency for {roleTitle}.";
                            nextAction = "Recommended learning and validation";
                        }
                        break;
                }
            }
            else if (skillProfilesById.TryGetValue(skillId, out var sp))
            {
                assessmentState = sp.FinalLevel;
                if (assessmentState.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
                {
                    preliminaryState = RoadmapNodeStates.Completed;
                    whyThisNode = $"Assessment verified advanced mastery in {skillName}.";
                    nextAction = "Apply advanced concepts in production architectures";
                }
                else
                {
                    preliminaryState = RoadmapNodeStates.NeedsDevelopment;
                    gapType = "ASSESSED GAP";
                    whyThisNode = $"Assessment showed {skillName} ({assessmentState}) requires targeted development.";
                    nextAction = "Review concepts and practice practical scenarios";
                }
            }
            else
            {
                assessmentState = "Not Assessed";
                if (rn.IsOptional)
                {
                    preliminaryState = RoadmapNodeStates.Optional;
                    gapType = "NONE";
                    whyThisNode = $"Optional elective competency for {roleTitle}.";
                    nextAction = "Elective study if relevant to specializations";
                }
                else if (rn.IsToolkitOnly)
                {
                    preliminaryState = RoadmapNodeStates.Available;
                    gapType = "NONE";
                    whyThisNode = $"Industry developer tooling supporting your professional workflow.";
                    nextAction = "Familiarize with tooling and collaboration standards";
                }
                else
                {
                    preliminaryState = RoadmapNodeStates.NotAssessed;
                    gapType = "ROLE COVERAGE GAP";
                    whyThisNode = $"Not assessed yet. This is a role competency for {roleTitle}.";
                    nextAction = "Recommended learning and validation";
                }
            }

            var prereqIds = prerequisitesByTarget.TryGetValue(skillId, out var pList)
                ? (IReadOnlyList<string>)pList
                : Array.Empty<string>();

            string requirement = rn.MandatoryFundamental
                ? "Mandatory Fundamental"
                : rn.IsToolkitOnly
                    ? "Career Toolkit"
                    : rn.IsOptional
                        ? "Optional Elective"
                        : rn.Category.Equals("core", StringComparison.OrdinalIgnoreCase)
                            ? "Core Competency"
                            : "Recommended Competency";

            nodeIntermediates.Add(new NodeEvaluationIntermediate(
                RoleNode: rn,
                CanonicalSkillId: skillId,
                Name: skillName,
                Classification: rn.CanonicalSkill.Classification,
                Requirement: requirement,
                PreliminaryState: preliminaryState,
                GapType: gapType,
                AssessmentState: assessmentState,
                WhyThisNode: whyThisNode,
                NextAction: nextAction,
                DisplayOrder: rn.DisplayOrder,
                IsToolkit: rn.IsToolkitOnly,
                IsOptional: rn.IsOptional,
                Category: rn.Category,
                Importance: rn.Importance,
                PrerequisiteSkillIds: prereqIds
            ));
        }

        // 5. Dependency / Prerequisite overlay
        // If a node has an incoming prerequisite whose state is NOT Completed, it becomes Locked.
        var nodeStates = nodeIntermediates.ToDictionary(n => n.CanonicalSkillId, n => n.PreliminaryState, StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodeIntermediates)
        {
            if (node.PreliminaryState == RoadmapNodeStates.Completed)
            {
                // Completed nodes remain Completed
                node.ResolvedState = RoadmapNodeStates.Completed;
                continue;
            }

            if (node.PrerequisiteSkillIds.Count > 0)
            {
                var unsatisfiedPrereq = node.PrerequisiteSkillIds
                    .FirstOrDefault(prereqId => !nodeStates.TryGetValue(prereqId, out var state) || state != RoadmapNodeStates.Completed);

                if (unsatisfiedPrereq != null)
                {
                    node.ResolvedState = RoadmapNodeStates.Locked;
                    var prereqName = nodesBySkillId.TryGetValue(unsatisfiedPrereq, out var prNode)
                        ? prNode.CanonicalSkill.DisplayName
                        : unsatisfiedPrereq;
                    node.WhyThisNode = $"Locked until prerequisite '{prereqName}' is completed.";
                    node.NextAction = $"Complete prerequisite '{prereqName}'";
                }
                else
                {
                    node.ResolvedState = node.PreliminaryState;
                }
            }
            else
            {
                node.ResolvedState = node.PreliminaryState;
            }
        }

        // 6. Priority policy: Select highest-priority actionable node(s) as "Current"
        // Candidates for "Current" must NOT be Completed, Locked, or Optional.
        // Candidates can be NeedsDevelopment, NotAssessed, or Available.
        var actionableCandidates = nodeIntermediates
            .Where(n => n.ResolvedState != RoadmapNodeStates.Completed &&
                        n.ResolvedState != RoadmapNodeStates.Locked &&
                        n.ResolvedState != RoadmapNodeStates.Optional)
            .OrderBy(n => GetPriorityWeight(n))
            .ThenBy(n => n.DisplayOrder)
            .ToList();

        var currentNodeIds = new List<string>();

        if (actionableCandidates.Count > 0)
        {
            // Pick top actionable candidate as Current
            var currentCandidate = actionableCandidates[0];
            currentCandidate.ResolvedState = RoadmapNodeStates.Current;
            currentNodeIds.Add(currentCandidate.CanonicalSkillId);

            if (currentCandidate.GapType == "ASSESSED GAP")
            {
                currentCandidate.WhyThisNode = $"Highest priority learning node: assessed gap in {currentCandidate.Name} requires focused practice.";
            }
            else if (currentCandidate.GapType == "EVIDENCE GAP")
            {
                currentCandidate.WhyThisNode = $"Immediate priority: interview lacked sufficient evidence for {currentCandidate.Name}.";
            }
            else if (currentCandidate.GapType == "ROLE COVERAGE GAP")
            {
                currentCandidate.WhyThisNode = $"Immediate priority: foundational role competency for {roleTitle} awaiting validation.";
            }
            else
            {
                currentCandidate.WhyThisNode = $"Immediate priority: next actionable milestone in {roleTitle} learning progression.";
            }

            // For any remaining candidates that were previously NeedsDevelopment or NotAssessed:
            // They keep their honest state (NeedsDevelopment or NotAssessed) or Available (if ready without gaps).
            for (int i = 1; i < actionableCandidates.Count; i++)
            {
                var candidate = actionableCandidates[i];
                if (candidate.ResolvedState == RoadmapNodeStates.Current)
                {
                    candidate.ResolvedState = candidate.PreliminaryState;
                }
            }
        }

        // 7. Assemble final DTOs
        var finalNodes = nodeIntermediates.Select(n => new PersonalizedRoadmapNodeDto(
            CanonicalSkillId: n.CanonicalSkillId,
            Name: n.Name,
            Classification: n.Classification,
            Requirement: n.Requirement,
            NodeState: n.ResolvedState ?? n.PreliminaryState,
            GapType: n.GapType,
            AssessmentState: n.AssessmentState,
            WhyThisNode: n.WhyThisNode,
            NextAction: n.NextAction,
            DisplayOrder: n.DisplayOrder,
            IsToolkit: n.IsToolkit,
            IsOptional: n.IsOptional,
            Category: n.Category,
            Importance: n.Importance,
            PrerequisiteSkillIds: n.PrerequisiteSkillIds
        )).ToList();

        // 8. Compute qualitative summary counts
        int completedCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.Completed);
        int needsDevCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.NeedsDevelopment);
        int notAssessedCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.NotAssessed);
        int availableCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.Available);
        int lockedCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.Locked);
        int optionalCount = finalNodes.Count(n => n.NodeState == RoadmapNodeStates.Optional);

        var summary = new RoadmapGraphSummaryDto(
            CurrentNodeIds: currentNodeIds,
            CompletedCount: completedCount,
            NeedsDevelopmentCount: needsDevCount,
            NotAssessedCount: notAssessedCount,
            AvailableCount: availableCount,
            LockedCount: lockedCount,
            OptionalCount: optionalCount,
            TotalNodes: finalNodes.Count
        );

        return new PersonalizedRoadmapGraphDto(
            RoleId: normalizedRoleId,
            RoleTitle: roleTitle,
            SessionId: sessionId,
            Nodes: finalNodes,
            Edges: edges,
            Summary: summary
        );
    }

    private static int GetPriorityWeight(NodeEvaluationIntermediate n)
    {
        // Lower number = higher priority
        // 1: Unresolved mandatory fundamental
        if (n.RoleNode.MandatoryFundamental)
        {
            if (n.GapType == "ASSESSED GAP") return 10;
            if (n.GapType == "EVIDENCE GAP") return 15;
            return 20; // ROLE COVERAGE GAP on mandatory fundamental
        }

        // 2: Core competency with assessed gap
        if (n.Category.Equals("core", StringComparison.OrdinalIgnoreCase))
        {
            if (n.GapType == "ASSESSED GAP") return 30;
            if (n.GapType == "EVIDENCE GAP") return 35;
            return 40; // ROLE COVERAGE GAP on core
        }

        // 3: Recommended competency with gap
        if (n.Category.Equals("recommended", StringComparison.OrdinalIgnoreCase))
        {
            if (n.GapType == "ASSESSED GAP") return 50;
            if (n.GapType == "EVIDENCE GAP") return 55;
            return 60;
        }

        // 4: Toolkit / other
        if (n.IsToolkit)
        {
            return 70;
        }

        // 5: Optional
        if (n.IsOptional)
        {
            return 90;
        }

        return 80;
    }

    private class NodeEvaluationIntermediate
    {
        public RoleRoadmapNode RoleNode { get; }
        public string CanonicalSkillId { get; }
        public string Name { get; }
        public string Classification { get; }
        public string Requirement { get; }
        public string PreliminaryState { get; set; }
        public string? ResolvedState { get; set; }
        public string GapType { get; set; }
        public string AssessmentState { get; set; }
        public string WhyThisNode { get; set; }
        public string NextAction { get; set; }
        public int DisplayOrder { get; }
        public bool IsToolkit { get; }
        public bool IsOptional { get; }
        public string Category { get; }
        public string Importance { get; }
        public IReadOnlyList<string> PrerequisiteSkillIds { get; }

        public NodeEvaluationIntermediate(
            RoleRoadmapNode RoleNode,
            string CanonicalSkillId,
            string Name,
            string Classification,
            string Requirement,
            string PreliminaryState,
            string GapType,
            string AssessmentState,
            string WhyThisNode,
            string NextAction,
            int DisplayOrder,
            bool IsToolkit,
            bool IsOptional,
            string Category,
            string Importance,
            IReadOnlyList<string> PrerequisiteSkillIds)
        {
            this.RoleNode = RoleNode;
            this.CanonicalSkillId = CanonicalSkillId;
            this.Name = Name;
            this.Classification = Classification;
            this.Requirement = Requirement;
            this.PreliminaryState = PreliminaryState;
            this.GapType = GapType;
            this.AssessmentState = AssessmentState;
            this.WhyThisNode = WhyThisNode;
            this.NextAction = NextAction;
            this.DisplayOrder = DisplayOrder;
            this.IsToolkit = IsToolkit;
            this.IsOptional = IsOptional;
            this.Category = Category;
            this.Importance = Importance;
            this.PrerequisiteSkillIds = PrerequisiteSkillIds;
        }
    }
}
