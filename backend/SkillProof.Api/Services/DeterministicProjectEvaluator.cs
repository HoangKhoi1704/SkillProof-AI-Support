using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicProjectEvaluator : IProjectEvaluator
{
    private static readonly Dictionary<string, string[]> SkillKeywordSignals =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["sql"] = new[] { "transaction", "isolation", "index", "explain", "lock", "concurrency", "acid", "query", "deadlock", "postgres", "foreign key" },
            ["testing"] = new[] { "test", "mock", "assert", "unit", "integration", "coverage", "xunit", "pytest", "boundary", "isolation" },
            ["system-design"] = new[] { "rate limit", "sliding window", "redis", "cache", "429", "circuit breaker", "resilience", "scale" },
            ["rest-api"] = new[] { "put", "patch", "idempotent", "etag", "rest", "endpoint", "openapi", "swagger", "status code", "json merge" },
            ["authentication-security"] = new[] { "jwt", "token", "refresh", "auth", "rsa", "hash", "rbac", "bearer", "signature" },
            ["programming-fundamentals"] = new[] { "async", "streaming", "cancellation", "bounded memory", "iasyncenumerable", "pipeline", "memory" },
            ["nosql"] = new[] { "document", "mongodb", "dynamo", "partition", "ttl", "eventual consistency", "secondary index" },
            ["caching"] = new[] { "cache", "cache-aside", "redis", "ttl", "stampede", "invalidation", "eviction" },
            ["csharp"] = new[] { "c#", "task", "async", "await", "record", "nullable", "linq", "pattern matching" },
            ["java"] = new[] { "java", "spring", "stream", "record", "completablefuture", "interface" },
            ["python"] = new[] { "python", "asyncio", "pydantic", "type hints", "dataclass", "fastapi" },
            ["cpp"] = new[] { "c++", "raii", "smart pointer", "move", "jthread", "thread" },
            ["javascript"] = new[] { "javascript", "node", "eventloop", "async", "promise", "middleware" },
            ["typescript"] = new[] { "typescript", "generic", "union", "discriminated", "zod", "interface" },
            ["go"] = new[] { "go", "golang", "goroutine", "channel", "context", "defer" },
            ["rust"] = new[] { "rust", "borrow", "ownership", "tokio", "async", "result", "crate" }
        };

    public Task<ProjectEvaluationDto> EvaluateAsync(
        GapBasedProjectDto project,
        SubmitProjectEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (request == null) throw new ArgumentNullException(nameof(request));

        var combinedEvidence = string.Join(" ", new[]
        {
            request.ProjectSummary,
            request.ImplementationExplanation,
            request.ArchitectureDecisions,
            request.TestingExplanation,
            string.Join(" ", request.EvidenceExcerpts ?? new List<string>())
        }).Trim();

        var skillEvidenceResults = new List<SkillEvidenceResultDto>();
        var requirementResults = new List<RequirementEvaluationResultDto>();
        var demonstratedEvidence = new List<string>();
        var missingEvidence = new List<string>();
        var improvementSuggestions = new List<string>();

        foreach (var targetedSkill in project.TargetedSkills)
        {
            var skillKey = targetedSkill.SkillId.Trim().ToLowerInvariant();
            var targetArea = targetedSkill.TargetArea;

            int matchCount = 0;
            var matchedKeywords = new List<string>();

            if (SkillKeywordSignals.TryGetValue(skillKey, out var signals))
            {
                foreach (var signal in signals)
                {
                    if (combinedEvidence.Contains(signal, StringComparison.OrdinalIgnoreCase))
                    {
                        matchCount++;
                        matchedKeywords.Add(signal);
                    }
                }
            }
            else
            {
                if (combinedEvidence.Contains(targetedSkill.SkillId, StringComparison.OrdinalIgnoreCase)) matchCount += 2;
                if (combinedEvidence.Contains(targetArea, StringComparison.OrdinalIgnoreCase)) matchCount += 2;
            }

            string status;
            var evidenceItems = new List<string>();
            var missingItems = new List<string>();

            if (string.IsNullOrWhiteSpace(combinedEvidence) || combinedEvidence.Length < 15 || matchCount == 0)
            {
                status = "Insufficient Evidence";
                var neutralMsg = $"Insufficient evidence was provided for {targetArea}.";
                missingItems.Add(neutralMsg);
                missingEvidence.Add(neutralMsg);
                improvementSuggestions.Add($"Provide concrete implementation details or code excerpts explaining your approach to {targetArea}.");
            }
            else if (matchCount >= 2 && combinedEvidence.Length >= 50)
            {
                status = "Demonstrated";
                var desc = $"Observed concrete engineering evidence addressing {targetArea} (key signals: {string.Join(", ", matchedKeywords.Take(3))}).";
                evidenceItems.Add(desc);
                demonstratedEvidence.Add(desc);
            }
            else
            {
                status = "Partially Demonstrated";
                var partialMsg = $"Partial evidence observed for {targetArea}; further elaboration on edge cases and failure modes recommended.";
                evidenceItems.Add(partialMsg);
                var neutralMissing = $"Insufficient evidence was provided for {targetArea} under failure edge cases.";
                missingItems.Add(neutralMissing);
                missingEvidence.Add(neutralMissing);
                improvementSuggestions.Add($"Deepen your technical explanation of {targetArea} by including automated test assertions and boundary handling.");
            }

            skillEvidenceResults.Add(new SkillEvidenceResultDto(
                SkillId: targetedSkill.SkillId,
                EvidenceStatus: status,
                Evidence: evidenceItems,
                MissingEvidence: missingItems
            ));
        }

        // Map requirement results
        foreach (var req in project.Requirements)
        {
            var skillResult = skillEvidenceResults.FirstOrDefault(s => s.SkillId.Equals(req.TargetsSkill, StringComparison.OrdinalIgnoreCase));
            var reqStatus = skillResult?.EvidenceStatus ?? "Insufficient Evidence";
            var reqNotes = reqStatus == "Demonstrated"
                ? $"Verified requirement addressing {req.TargetsSkill} with substantive evidence."
                : (reqStatus == "Partially Demonstrated"
                    ? $"Partial implementation noted for {req.TargetsSkill}."
                    : $"Insufficient evidence provided for requirement targeting {req.TargetsSkill}.");

            requirementResults.Add(new RequirementEvaluationResultDto(
                Requirement: req.Requirement,
                TargetsSkill: req.TargetsSkill,
                Status: reqStatus,
                EvaluationNotes: reqNotes
            ));
        }

        // Compute overall qualitative status
        string overallStatus;
        if (skillEvidenceResults.All(s => s.EvidenceStatus == "Demonstrated"))
        {
            overallStatus = "Demonstrated";
        }
        else if (skillEvidenceResults.Any(s => s.EvidenceStatus == "Demonstrated" || s.EvidenceStatus == "Partially Demonstrated"))
        {
            overallStatus = "Partially Demonstrated";
        }
        else
        {
            overallStatus = "Insufficient Evidence";
        }

        // Deterministic Claim Safety Gate
        var portfolioProof = GenerateGatedProof(project, skillEvidenceResults);

        var evaluation = new ProjectEvaluationDto(
            ProjectId: project.ProjectId,
            OverallStatus: overallStatus,
            SkillEvidence: skillEvidenceResults,
            RequirementResults: requirementResults,
            DemonstratedEvidence: demonstratedEvidence,
            MissingEvidence: missingEvidence,
            ImprovementSuggestions: improvementSuggestions,
            PortfolioProof: portfolioProof
        );

        return Task.FromResult(evaluation);
    }

    public static PortfolioProofDto GenerateGatedProof(
        GapBasedProjectDto project,
        List<SkillEvidenceResultDto> skillEvidence)
    {
        var demonstratedSkills = new List<string>();
        var portfolioBullets = new List<string>();
        var cvBullets = new List<string>();
        var evidenceNotes = new List<string>();

        foreach (var item in skillEvidence)
        {
            var skillTarget = project.TargetedSkills.FirstOrDefault(s => s.SkillId.Equals(item.SkillId, StringComparison.OrdinalIgnoreCase));
            var targetArea = skillTarget?.TargetArea ?? item.SkillId;

            if (item.EvidenceStatus == "Demonstrated")
            {
                demonstratedSkills.Add(item.SkillId);
                portfolioBullets.Add($"Engineered {targetArea} for {project.Title}, demonstrating verified architectural implementation and test coverage.");
                cvBullets.Add($"Architected and implemented {targetArea} in {project.Title} with verified automated testing and error isolation.");
                evidenceNotes.Add($"Demonstrated verified technical competency in {targetArea}.");
            }
            else if (item.EvidenceStatus == "Partially Demonstrated")
            {
                // Partially Demonstrated produces cautious notes ONLY, NO strong CV bullets!
                evidenceNotes.Add($"Partial evidence submitted for {targetArea}; additional integration verification or benchmark analysis recommended prior to CV inclusion.");
            }
            else
            {
                // Insufficient Evidence NEVER generates a positive skill claim or CV bullet!
                evidenceNotes.Add($"Insufficient evidence was provided for {targetArea}; excluded from portfolio proof.");
            }
        }

        string summary;
        if (demonstratedSkills.Count > 0)
        {
            summary = $"Completed {project.Title} with verified demonstrated evidence across {string.Join(", ", demonstratedSkills)}.";
        }
        else
        {
            summary = $"Project evidence submitted for {project.Title}; no verified portfolio claims could be issued due to insufficient submitted proof.";
        }

        return new PortfolioProofDto(
            ProjectTitle: project.Title,
            Summary: summary,
            DemonstratedSkills: demonstratedSkills,
            PortfolioBullets: portfolioBullets,
            CvBullets: cvBullets,
            EvidenceNotes: evidenceNotes
        );
    }
}
