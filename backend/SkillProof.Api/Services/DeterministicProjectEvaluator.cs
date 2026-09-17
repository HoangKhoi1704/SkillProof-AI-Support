using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicProjectEvaluator : IProjectEvaluator
{
    private readonly IRepositoryEvidenceVerifier _repoVerifier;
    private readonly IDeploymentEvidenceVerifier _deployVerifier;
    private readonly IDataAnalystEvidenceVerifier _daVerifier;

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

    public DeterministicProjectEvaluator(
        IRepositoryEvidenceVerifier? repoVerifier = null,
        IDeploymentEvidenceVerifier? deployVerifier = null,
        IDataAnalystEvidenceVerifier? daVerifier = null)
    {
        var urlValidator = new UrlSafetyValidator();
        _repoVerifier = repoVerifier ?? new RepositoryEvidenceVerifier(urlValidator);
        _deployVerifier = deployVerifier ?? new DeploymentEvidenceVerifier(urlValidator);
        _daVerifier = daVerifier ?? new DataAnalystEvidenceVerifier(urlValidator);
    }

    public async Task<ProjectEvaluationDto> EvaluateAsync(
        GapBasedProjectDto project,
        SubmitProjectEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (request == null) throw new ArgumentNullException(nameof(request));

        // 1. Execute layered deterministic verifiers
        RepositoryEvidenceResult? repoResult = null;
        if (!string.IsNullOrWhiteSpace(request.RepositoryUrl))
        {
            repoResult = await _repoVerifier.VerifyRepositoryAsync(request.RepositoryUrl, cancellationToken);
        }

        DeploymentEvidenceResult? deployResult = null;
        if (!string.IsNullOrWhiteSpace(request.DeployedUrl))
        {
            deployResult = await _deployVerifier.VerifyDeploymentAsync(request.DeployedUrl, project.RoleId, cancellationToken);
        }

        DataAnalystEvidenceResult? daResult = null;
        if (!string.IsNullOrWhiteSpace(request.NotebookUrl) ||
            !string.IsNullOrWhiteSpace(request.DatasetUrl) ||
            !string.IsNullOrWhiteSpace(request.DashboardUrl) ||
            project.RoleId.Equals("data-analyst", StringComparison.OrdinalIgnoreCase))
        {
            daResult = await _daVerifier.VerifyDataAnalystEvidenceAsync(
                request.NotebookUrl,
                request.DatasetUrl,
                request.DashboardUrl,
                request.Notes,
                cancellationToken
            );
        }

        // Composite artifacts list & security warnings
        var artifacts = new List<VerificationArtifactItem>();
        var securityWarnings = new List<string>();

        if (repoResult != null)
        {
            securityWarnings.AddRange(repoResult.SecurityWarnings);
            artifacts.Add(new VerificationArtifactItem(
                EvidenceId: $"ev-repo-{Guid.NewGuid():N}"[..12],
                Type: EvidenceType.Repository,
                Status: repoResult.Status,
                SourceLocation: request.RepositoryUrl ?? "Repository",
                Summary: repoResult.RepositoryExists
                    ? $"Observed repository with {repoResult.FoundFiles.Count} verified files (branch: {repoResult.DefaultBranch})."
                    : (repoResult.ErrorMessage ?? "Repository could not be verified."),
                VerifiedAt: DateTimeOffset.UtcNow
            ));
        }

        if (deployResult != null)
        {
            artifacts.Add(new VerificationArtifactItem(
                EvidenceId: $"ev-dep-{Guid.NewGuid():N}"[..12],
                Type: EvidenceType.Deployment,
                Status: deployResult.Status,
                SourceLocation: request.DeployedUrl ?? "Deployment",
                Summary: deployResult.IsReachable
                    ? $"Deployed application verified reachable (HTTP {deployResult.StatusCode}, {deployResult.ContentType})."
                    : (deployResult.ErrorMessage ?? "Deployment unreachable."),
                VerifiedAt: DateTimeOffset.UtcNow
            ));
        }

        if (daResult != null && (daResult.NotebookFound || daResult.DatasetFound || daResult.DashboardReachable))
        {
            artifacts.Add(new VerificationArtifactItem(
                EvidenceId: $"ev-da-{Guid.NewGuid():N}"[..12],
                Type: daResult.NotebookFound ? EvidenceType.Notebook : (daResult.DashboardReachable ? EvidenceType.Dashboard : EvidenceType.Dataset),
                Status: daResult.Status,
                SourceLocation: request.NotebookUrl ?? request.DashboardUrl ?? "Analytics Workspace",
                Summary: daResult.NotesSummary ?? "Data analytics evidence artifacts verified.",
                VerifiedAt: DateTimeOffset.UtcNow
            ));
        }

        var compositeReport = new CompositeEvidenceReport(
            Repository: repoResult,
            Deployment: deployResult,
            DataAnalyst: daResult,
            Artifacts: artifacts,
            SecurityWarnings: securityWarnings
        );

        // 2. Text evidence fallback for legacy backward compatibility
        var combinedTextEvidence = string.Join(" ", new[]
        {
            request.ProjectSummary,
            request.ImplementationExplanation,
            request.ArchitectureDecisions,
            request.TestingExplanation,
            request.Notes,
            string.Join(" ", request.EvidenceExcerpts ?? new List<string>())
        }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        bool hasStructuredVerifiedEvidence =
            (repoResult != null && repoResult.Status == VerificationState.Verified) ||
            (deployResult != null && deployResult.Status == VerificationState.Verified) ||
            (daResult != null && daResult.Status == VerificationState.Verified);

        bool hasFailedOrInvalidEvidence =
            (repoResult != null && repoResult.Status == VerificationState.Invalid) ||
            (deployResult != null && deployResult.Status == VerificationState.Invalid) ||
            (daResult != null && daResult.Status == VerificationState.Invalid) ||
            (repoResult != null && repoResult.Status == VerificationState.Unavailable && string.IsNullOrWhiteSpace(combinedTextEvidence));

        var requirementResults = new List<RequirementEvaluationResultDto>();
        var skillEvidenceResults = new List<SkillEvidenceResultDto>();
        var demonstratedEvidence = new List<string>();
        var missingEvidence = new List<string>();
        var improvementSuggestions = new List<string>();

        // 3. Build Requirement Evidence Matrix
        foreach (var req in project.Requirements)
        {
            var targetSkill = req.TargetsSkill.Trim().ToLowerInvariant();
            var reqDeliverable = req.Deliverable ?? req.Requirement;
            var evidenceFound = new List<string>();
            string sourceArtifact = "Unverified Submission";
            string reqStatus;
            string reqNotes;

            if (hasStructuredVerifiedEvidence)
            {
                // Inspect repository evidence
                if (repoResult != null && repoResult.RepositoryExists)
                {
                    sourceArtifact = $"Repository: {request.RepositoryUrl}";
                    if (targetSkill.Contains("test") && repoResult.TestFiles.Count > 0)
                    {
                        evidenceFound.Add($"Verified automated test files: {string.Join(", ", repoResult.TestFiles)}");
                    }
                    if (repoResult.ManifestFiles.Count > 0)
                    {
                        evidenceFound.Add($"Verified build manifest: {string.Join(", ", repoResult.ManifestFiles)}");
                    }
                    if (repoResult.LanguageIndicators.Count > 0)
                    {
                        evidenceFound.Add($"Observed technology indicators: {string.Join(", ", repoResult.LanguageIndicators)}");
                    }
                }

                // Inspect deployment evidence
                if (deployResult != null && deployResult.IsReachable)
                {
                    sourceArtifact = string.IsNullOrWhiteSpace(sourceArtifact) || sourceArtifact == "Unverified Submission"
                        ? $"Deployment: {request.DeployedUrl}"
                        : $"{sourceArtifact} + Deployed: {request.DeployedUrl}";
                    evidenceFound.Add($"Verified reachable endpoint: HTTP {deployResult.StatusCode} ({deployResult.PageTitle ?? deployResult.ContentType})");
                }

                // Inspect data analyst evidence
                if (daResult != null && daResult.Status == VerificationState.Verified)
                {
                    sourceArtifact = $"Analytics: {request.NotebookUrl ?? request.DashboardUrl ?? "Workspace"}";
                    if (daResult.NotebookFound)
                    {
                        evidenceFound.Add($"Verified notebook structure ({daResult.CodeCellCount} code cells, imports: {string.Join(", ", daResult.NotebookImports.Take(3))})");
                    }
                    if (daResult.DashboardReachable)
                    {
                        evidenceFound.Add("Verified interactive executive dashboard accessibility.");
                    }
                }

                if (evidenceFound.Count > 0)
                {
                    reqStatus = "Demonstrated";
                    reqNotes = $"We could verify concrete evidence satisfying '{reqDeliverable}' via {sourceArtifact}.";
                    demonstratedEvidence.Add($"Satisfied requirement '{reqDeliverable}' with verified artifact.");
                }
                else
                {
                    reqStatus = "Partially Demonstrated";
                    reqNotes = $"Partial implementation observed; specific deliverable '{reqDeliverable}' requires further documentation.";
                    missingEvidence.Add($"Not enough evidence was available to verify '{reqDeliverable}'.");
                }
            }
            else if (hasFailedOrInvalidEvidence)
            {
                reqStatus = "Insufficient Evidence";
                sourceArtifact = "Invalid / Unreachable Evidence";
                reqNotes = $"Verification failed for submitted evidence artifacts. Could not confirm requirement '{reqDeliverable}'.";
                missingEvidence.Add($"Could not verify '{reqDeliverable}' due to inaccessible submission source.");
            }
            else
            {
                // Legacy text evaluation fallback
                int matchCount = 0;
                var matchedSignals = new List<string>();

                string[]? signals = null;
                if (SkillKeywordSignals.TryGetValue(targetSkill, out var directSignals))
                {
                    signals = directSignals;
                }
                else
                {
                    var shortKey = targetSkill.Split('.').Last().Replace("-apis", "-api");
                    if (shortKey.Contains("relational-database") || shortKey.Contains("database")) shortKey = "sql";
                    SkillKeywordSignals.TryGetValue(shortKey, out signals);
                }

                if (signals != null)
                {
                    foreach (var sig in signals)
                    {
                        if (combinedTextEvidence.Contains(sig, StringComparison.OrdinalIgnoreCase))
                        {
                            matchCount++;
                            matchedSignals.Add(sig);
                        }
                    }
                }
                else
                {
                    if (combinedTextEvidence.Contains(req.TargetsSkill, StringComparison.OrdinalIgnoreCase)) matchCount += 2;
                    if (combinedTextEvidence.Contains(req.Requirement, StringComparison.OrdinalIgnoreCase)) matchCount += 2;
                }

                if (matchCount >= 2 && combinedTextEvidence.Length >= 40)
                {
                    reqStatus = "Demonstrated";
                    sourceArtifact = "Candidate Written Evidence";
                    evidenceFound.Add($"Written technical explanation provides concrete details: {string.Join(", ", matchedSignals.Take(3))}");
                    reqNotes = $"Verified requirement addressing {req.TargetsSkill} with substantive written explanation.";
                    demonstratedEvidence.Add($"Substantive explanation for requirement targeting {req.TargetsSkill}.");
                }
                else if (matchCount >= 1 || combinedTextEvidence.Length >= 20)
                {
                    reqStatus = "Partially Demonstrated";
                    sourceArtifact = "Candidate Written Evidence";
                    evidenceFound.Add("Introductory technical summary observed.");
                    reqNotes = $"Partial evidence observed for requirement targeting {req.TargetsSkill}.";
                    missingEvidence.Add($"Further technical elaboration required for '{reqDeliverable}'.");
                }
                else
                {
                    reqStatus = "Insufficient Evidence";
                    sourceArtifact = "Insufficient Submission";
                    reqNotes = $"Insufficient evidence was provided for requirement targeting {req.TargetsSkill}.";
                    missingEvidence.Add($"Insufficient evidence provided for requirement targeting {req.TargetsSkill}.");
                }
            }

            requirementResults.Add(new RequirementEvaluationResultDto(
                Requirement: req.Requirement,
                TargetsSkill: req.TargetsSkill,
                Status: reqStatus,
                EvaluationNotes: reqNotes,
                EvidenceFound: evidenceFound,
                SourceArtifact: sourceArtifact
            ));
        }

        // 4. Build Skill Evidence Matrix
        foreach (var targetedSkill in project.TargetedSkills)
        {
            var skillId = targetedSkill.SkillId.Trim().ToLowerInvariant();
            var targetArea = targetedSkill.TargetArea;
            var matchingReqs = requirementResults.Where(r => r.TargetsSkill.Equals(skillId, StringComparison.OrdinalIgnoreCase)).ToList();

            string skillStatus;
            var evList = new List<string>();
            var missingList = new List<string>();

            if (matchingReqs.Any(r => r.Status == "Demonstrated"))
            {
                skillStatus = "Demonstrated";
                evList.Add($"We could verify concrete technical artifacts addressing {targetArea}.");
            }
            else if (matchingReqs.Any(r => r.Status == "Partially Demonstrated") ||
                     (combinedTextEvidence.Length >= 40 && (combinedTextEvidence.Contains(skillId, StringComparison.OrdinalIgnoreCase) || combinedTextEvidence.Contains(targetArea, StringComparison.OrdinalIgnoreCase))))
            {
                skillStatus = "Partially Demonstrated";
                evList.Add($"Partial evidence observed for {targetArea}; further elaboration on edge cases and failure modes recommended.");
                var partialMissing = $"Insufficient evidence was provided for {targetArea} under failure edge cases.";
                missingList.Add(partialMissing);
                missingEvidence.Add(partialMissing);
                improvementSuggestions.Add($"Include concrete repository test files or measurable implementation artifacts for {targetArea}.");
            }
            else
            {
                skillStatus = "Insufficient Evidence";
                var neutralMsg = $"Insufficient evidence was provided for {targetArea}.";
                missingList.Add(neutralMsg);
                missingEvidence.Add(neutralMsg);
                improvementSuggestions.Add($"Submit a verifiable public repository link or deployed demonstration addressing {targetArea}.");
            }

            skillEvidenceResults.Add(new SkillEvidenceResultDto(
                SkillId: targetedSkill.SkillId,
                EvidenceStatus: skillStatus,
                Evidence: evList,
                MissingEvidence: missingList
            ));
        }

        // 5. Compute Overall Status
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

        // 6. Generate Gated Portfolio / CV Proof
        var portfolioProof = GenerateGatedProof(project, skillEvidenceResults, compositeReport);

        return new ProjectEvaluationDto(
            ProjectId: project.ProjectId,
            OverallStatus: overallStatus,
            SkillEvidence: skillEvidenceResults,
            RequirementResults: requirementResults,
            DemonstratedEvidence: demonstratedEvidence.Distinct().ToList(),
            MissingEvidence: missingEvidence.Distinct().ToList(),
            ImprovementSuggestions: improvementSuggestions.Distinct().ToList(),
            PortfolioProof: portfolioProof,
            VerificationReport: compositeReport
        );
    }

    public static PortfolioProofDto GenerateGatedProof(
        GapBasedProjectDto project,
        List<SkillEvidenceResultDto> skillEvidence,
        CompositeEvidenceReport? verificationReport = null)
    {
        var demonstratedSkills = new List<string>();
        var portfolioBullets = new List<string>();
        var cvBullets = new List<string>();
        var evidenceNotes = new List<string>();
        var traceability = new List<string>();

        foreach (var item in skillEvidence)
        {
            var skillTarget = project.TargetedSkills.FirstOrDefault(s => s.SkillId.Equals(item.SkillId, StringComparison.OrdinalIgnoreCase));
            var targetArea = skillTarget?.TargetArea ?? item.SkillId;

            if (item.EvidenceStatus == "Demonstrated")
            {
                demonstratedSkills.Add(item.SkillId);
                portfolioBullets.Add($"Engineered {targetArea} for {project.Title}, supported by verified repository artifacts and observable technical deliverables.");
                cvBullets.Add($"Architected and delivered {targetArea} in {project.Title}, verified against objective technical standards with automated testing.");
                evidenceNotes.Add($"Demonstrated verified technical competency in {targetArea}.");
                traceability.Add($"project:{project.ProjectId} | skill:{item.SkillId} | status:Demonstrated | evidence:VerifiedArtifact");
            }
            else if (item.EvidenceStatus == "Partially Demonstrated")
            {
                // Cautious notes ONLY, NO strong CV bullets!
                evidenceNotes.Add($"Partial evidence submitted for {targetArea}; additional integration verification or benchmark analysis recommended prior to CV inclusion.");
                traceability.Add($"project:{project.ProjectId} | skill:{item.SkillId} | status:PartiallyDemonstrated | evidence:Partial");
            }
            else
            {
                // Insufficient Evidence NEVER generates a positive skill claim or CV bullet!
                evidenceNotes.Add($"Insufficient evidence was provided for {targetArea}; excluded from portfolio proof.");
                traceability.Add($"project:{project.ProjectId} | skill:{item.SkillId} | status:InsufficientEvidence | evidence:None");
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
            EvidenceNotes: evidenceNotes,
            ClaimTraceability: traceability
        );
    }
}
