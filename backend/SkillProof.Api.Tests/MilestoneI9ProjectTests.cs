using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneI9ProjectTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;
    private readonly DeterministicProjectRecommender _deterministicRecommender;
    private readonly OpenAiProjectRecommender _openAiRecommender;
    private readonly DeterministicProjectEvaluator _deterministicEvaluator;

    public MilestoneI9ProjectTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _deterministicRecommender = new DeterministicProjectRecommender();
        _deterministicEvaluator = new DeterministicProjectEvaluator();
        _openAiRecommender = new OpenAiProjectRecommender(
            apiKey: "dummy-key",
            model: "gpt-5.4-mini",
            fallbackRecommender: _deterministicRecommender,
            logger: NullLogger<OpenAiProjectRecommender>.Instance
        );
    }

    private static ProjectGapContext CreateSampleGapContext()
    {
        return new ProjectGapContext(
            RoleId: "backend-developer",
            TargetSkills: new List<ProjectGapTargetSkill>
            {
                new("sql", "Intermediate", "Transaction Isolation & Concurrency", "Propose concurrency strategy", "ADR and execution plan"),
                new("testing", "Beginner", "Integration Testing & Boundary Mocking", "Build test suite", "Automated xUnit suite"),
                new("caching", "Insufficient Evidence", "Distributed Cache Invalidation", "Implement cache-aside", "Cache telemetry report")
            }
        );
    }

    [Fact]
    public async Task Test01_RecommendGapBasedProject_FromTrustedContext_Succeeds()
    {
        var context = CreateSampleGapContext();
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);

        Assert.NotNull(project);
        Assert.False(string.IsNullOrWhiteSpace(project.ProjectId));
        Assert.Equal("backend-developer", project.RoleId);
        Assert.False(string.IsNullOrWhiteSpace(project.Title));
        Assert.False(string.IsNullOrWhiteSpace(project.Scenario));
        Assert.False(string.IsNullOrWhiteSpace(project.Objective));
        Assert.False(string.IsNullOrWhiteSpace(project.PortfolioOutcome));

        // Exactly bounded requirements matching target skills
        Assert.InRange(project.Requirements.Count, 3, 6);
        Assert.NotEmpty(project.Deliverables);
        Assert.NotEmpty(project.EvaluationCriteria);
        Assert.NotEmpty(project.EvidenceRequirements);

        // Every requirement must trace directly to a target skill
        var targetSkillIds = context.TargetSkills.Select(s => s.SkillId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var req in project.Requirements)
        {
            Assert.Contains(req.TargetsSkill, targetSkillIds);
            Assert.False(string.IsNullOrWhiteSpace(req.Requirement));
            Assert.False(string.IsNullOrWhiteSpace(req.Deliverable));
        }
    }

    [Fact]
    public void Test02_RecommendGapBasedProject_RejectsArbitrarySkills_InAiValidation()
    {
        var context = CreateSampleGapContext();

        // AI returns a requirement targeting an arbitrary unassessed skill ("machine-learning")
        var aiRawJson = """
        {
          "title": "AI Driven Inventory",
          "scenario": "A high concurrency ordering platform",
          "objective": "Build microservices",
          "requirements": [
            {
              "requirement": "Train deep learning model for churn prediction",
              "targetsSkill": "machine-learning",
              "deliverable": "Model weights"
            },
            {
              "requirement": "Implement pessimistic concurrency locks",
              "targetsSkill": "sql",
              "deliverable": "SQL migration"
            }
          ],
          "deliverables": ["Deliverable 1"],
          "evidenceRequirements": ["Working code"],
          "evaluationCriteria": ["Clean architecture"],
          "portfolioOutcome": "Strong backend portfolio project"
        }
        """;

        var project = _openAiRecommender.ValidateAndProcessGapBasedAiResponse(context, aiRawJson);
        Assert.NotNull(project);

        // The arbitrary "machine-learning" requirement MUST be filtered out
        Assert.DoesNotContain(project.Requirements, r => r.TargetsSkill.Equals("machine-learning", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(project.Requirements, r => r.TargetsSkill.Equals("sql", StringComparison.OrdinalIgnoreCase));

        // All 3 expected skills from context must be represented
        var coveredSkills = project.Requirements.Select(r => r.TargetsSkill).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("sql", coveredSkills);
        Assert.Contains("testing", coveredSkills);
        Assert.Contains("caching", coveredSkills);
    }

    [Fact]
    public async Task Test03_RecommendGapBasedProject_DeterministicFallback_OnInvalidJson()
    {
        var context = CreateSampleGapContext();
        var invalidJson = "{ malformed json ...";

        var project = _openAiRecommender.ValidateAndProcessGapBasedAiResponse(context, invalidJson);
        Assert.Null(project); // Triggers fallback to deterministic

        var fallbackProject = await _deterministicRecommender.RecommendGapBasedAsync(context);
        Assert.NotNull(fallbackProject);
        Assert.Equal("backend-developer", fallbackProject.RoleId);
        Assert.NotEmpty(fallbackProject.Requirements);
    }

    [Fact]
    public async Task Test04_Endpoint_RecommendAdaptiveProject_UsingCompletedSession()
    {
        // 1. Start adaptive session
        var startReq = new AdaptiveSessionRequest("backend-developer", new List<string> { "sql", "testing" }, "csharp");
        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        Assert.Equal(HttpStatusCode.OK, startRes.StatusCode);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);

        // 2. Answer questions to complete session
        var a1 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(session.CurrentQuestion!.Id, "Explain analyze and composite index on foreign keys with Repeatable Read isolation."));
        var s1 = await a1.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a2 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s1!.CurrentQuestion!.Id, "Isolation levels and concurrency trade-offs with serializable transactions."));
        var s2 = await a2.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a3 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s2!.CurrentQuestion!.Id, "Mock payment gateway with xUnit and verify boundary edge cases."));
        var s3 = await a3.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a4 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s3!.CurrentQuestion!.Id, "Unit test invariants and integration tests with test doubles."));
        var s4 = await a4.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a5 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s4!.CurrentQuestion!.Id, "Async await in C# with cancellation tokens and task synchronization."));
        var s5 = await a5.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(s5);

        var a6 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s5.CurrentQuestion!.Id, "Concurrent collections and thread-safe lock-free synchronization patterns."));
        var s6 = await a6.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.Equal("completed", s6!.Status);

        // 3. Recommend project via endpoint
        var projRes = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/project/recommend", null);
        Assert.Equal(HttpStatusCode.OK, projRes.StatusCode);
        var project = await projRes.Content.ReadFromJsonAsync<GapBasedProjectDto>();

        Assert.NotNull(project);
        Assert.False(string.IsNullOrWhiteSpace(project.Title));
        Assert.False(string.IsNullOrWhiteSpace(project.Scenario));
        Assert.NotEmpty(project.Requirements);
        Assert.NotEmpty(project.Deliverables);

        // 4. Retrieve project via GET endpoint
        var getProj = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/project");
        Assert.Equal(HttpStatusCode.OK, getProj.StatusCode);
    }

    [Fact]
    public async Task Test05_Endpoint_RecommendAdaptiveProject_BeforeCompletion_ReturnsBadRequest()
    {
        var startReq = new AdaptiveSessionRequest("backend-developer", new List<string> { "sql" });
        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var projRes = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session!.SessionId}/project/recommend", null);
        Assert.Equal(HttpStatusCode.BadRequest, projRes.StatusCode);
    }

    [Fact]
    public async Task Test06_Endpoint_RecommendAdaptiveProject_NonExistentSession_ReturnsNotFound()
    {
        var projRes = await _client.PostAsync("/api/diagnostics/adaptive/sessions/non-existent-session-id/project/recommend", null);
        Assert.Equal(HttpStatusCode.NotFound, projRes.StatusCode);
    }

    [Fact]
    public async Task Test07_EvaluateProjectEvidence_DemonstratedStatus_And_PortfolioBullets()
    {
        var context = CreateSampleGapContext();
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);

        var submission = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://github.com/candidate/resilient-inventory",
            ProjectSummary: "Engineered a production-ready inventory reservation service with strict database concurrency controls and automated test isolation.",
            ImplementationExplanation: "Implemented explicit database transaction management using Repeatable Read isolation. Added composite index on (ProductId, Status) and verified query execution plan using EXPLAIN ANALYZE to eliminate sequential table scans. Configured Redis cache with cache-aside pattern and TTL eviction.",
            ArchitectureDecisions: "Selected Repeatable Read to prevent phantom reads during inventory subtraction without the high lock overhead of full serialization. Used distributed cache invalidation to prevent stampedes.",
            TestingExplanation: "Implemented an automated test suite using xUnit and Moq to mock third-party payment gateways. Verified boundary conditions and concurrency contention edge cases."
        );

        var evaluation = await _deterministicEvaluator.EvaluateAsync(project, submission);

        Assert.NotNull(evaluation);
        Assert.Equal(project.ProjectId, evaluation.ProjectId);
        Assert.Equal("Demonstrated", evaluation.OverallStatus);
        Assert.NotEmpty(evaluation.DemonstratedEvidence);

        // Skill Evidence checks
        var sqlEv = evaluation.SkillEvidence.FirstOrDefault(s => s.SkillId == "sql");
        Assert.NotNull(sqlEv);
        Assert.Equal("Demonstrated", sqlEv.EvidenceStatus);

        var testingEv = evaluation.SkillEvidence.FirstOrDefault(s => s.SkillId == "testing");
        Assert.NotNull(testingEv);
        Assert.Equal("Demonstrated", testingEv.EvidenceStatus);

        // Portfolio Proof checks
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.Contains("sql", evaluation.PortfolioProof.DemonstratedSkills);
        Assert.Contains("testing", evaluation.PortfolioProof.DemonstratedSkills);
        Assert.NotEmpty(evaluation.PortfolioProof.PortfolioBullets);
        Assert.NotEmpty(evaluation.PortfolioProof.CvBullets);
    }

    [Fact]
    public async Task Test08_EvaluateProjectEvidence_PartiallyDemonstrated_DoesNotGenerateStrongCvBullets()
    {
        var context = new ProjectGapContext(
            RoleId: "backend-developer",
            TargetSkills: new List<ProjectGapTargetSkill>
            {
                new("sql", "Intermediate", "Transaction Concurrency", "Practice task", "Target")
            }
        );
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);

        // Exactly 1 SQL signal ("query") with no advanced concurrency, indexing, or transaction depth
        var submission = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://github.com/candidate/sql-demo",
            ProjectSummary: "Created a simple data access service.",
            ImplementationExplanation: "I wrote a basic query to fetch records.",
            ArchitectureDecisions: "Kept the architecture simple and lightweight.",
            TestingExplanation: "Manually verified by calling endpoints."
        );

        var evaluation = await _deterministicEvaluator.EvaluateAsync(project, submission);

        Assert.NotNull(evaluation);
        var sqlEv = evaluation.SkillEvidence.First(s => s.SkillId == "sql");
        Assert.Equal("Partially Demonstrated", sqlEv.EvidenceStatus);

        // Claim safety gate: Partially Demonstrated produces notes only, NO strong CV claims!
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.Empty(evaluation.PortfolioProof.CvBullets);
        Assert.NotEmpty(evaluation.PortfolioProof.EvidenceNotes);
        Assert.Contains(evaluation.PortfolioProof.EvidenceNotes, n => n.Contains("Partial evidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Test09_EvaluateProjectEvidence_InsufficientEvidence_NeutralSemantics()
    {
        var context = new ProjectGapContext(
            RoleId: "backend-developer",
            TargetSkills: new List<ProjectGapTargetSkill>
            {
                new("sql", "Intermediate", "Transaction Isolation", "Practice task", "Target")
            }
        );
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);

        // Empty submission
        var submission = new SubmitProjectEvidenceRequest(
            RepositoryUrl: null,
            ProjectSummary: "",
            ImplementationExplanation: "",
            ArchitectureDecisions: "",
            TestingExplanation: ""
        );

        var evaluation = await _deterministicEvaluator.EvaluateAsync(project, submission);

        Assert.NotNull(evaluation);
        Assert.Equal("Insufficient Evidence", evaluation.OverallStatus);

        var sqlEv = evaluation.SkillEvidence.First(s => s.SkillId == "sql");
        Assert.Equal("Insufficient Evidence", sqlEv.EvidenceStatus);

        // MUST use neutral phrasing: "Insufficient evidence was provided for [Target Area]"
        Assert.NotEmpty(sqlEv.MissingEvidence);
        Assert.Contains("Insufficient evidence was provided for", sqlEv.MissingEvidence[0]);
        Assert.DoesNotContain("failed", sqlEv.MissingEvidence[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cannot", sqlEv.MissingEvidence[0], StringComparison.OrdinalIgnoreCase);

        // Gated Claim: Zero portfolio/CV claims
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.Empty(evaluation.PortfolioProof.PortfolioBullets);
        Assert.Empty(evaluation.PortfolioProof.CvBullets);
        Assert.Empty(evaluation.PortfolioProof.DemonstratedSkills);
    }

    [Fact]
    public void Test10_ClaimSafetyGate_NeverInventsMetricsOrTechnologies()
    {
        var context = CreateSampleGapContext();
        var project = new GapBasedProjectDto(
            ProjectId: "p1",
            RoleId: "backend-developer",
            Title: "Order Reservation Service",
            Scenario: "E-commerce platform",
            Objective: "Build inventory reservation",
            TargetedSkills: new List<TargetedSkillDto> { new("sql", "Intermediate", "Transaction Isolation", "Why") },
            Requirements: new List<ProjectRequirementDto> { new("Protect inventory updates", "sql", "DDL", "sql") },
            Deliverables: new List<string> { "DDL" },
            EvidenceRequirements: new List<string> { "Code" },
            EvaluationCriteria: new List<string> { "Correct isolation" },
            PortfolioOutcome: "Strong portfolio piece"
        );

        var skillEvidence = new List<SkillEvidenceResultDto>
        {
            new("sql", "Demonstrated", new List<string> { "Used Repeatable Read transaction" }, new List<string>())
        };

        var proof = DeterministicProjectEvaluator.GenerateGatedProof(project, skillEvidence);

        Assert.NotNull(proof);
        Assert.NotEmpty(proof.CvBullets);

        // Ensure no fabricated numbers, percentages, or metrics
        foreach (var bullet in proof.CvBullets)
        {
            Assert.DoesNotMatch(@"\b\d{1,3}%", bullet);
            Assert.DoesNotMatch(@"\b\d+k users\b", bullet);
            Assert.DoesNotMatch(@"\b\d+x faster\b", bullet);
        }
    }

    [Fact]
    public async Task Test11_ProfileFinalLevels_RemainUnmutated_AfterProjectEvaluation()
    {
        // 1. Complete an adaptive session
        var startReq = new AdaptiveSessionRequest("backend-developer", new List<string> { "sql" });
        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a1 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session!.SessionId}/answers",
            new AdaptiveAnswerRequest(session.CurrentQuestion!.Id, "Explain analyze and composite indexes with Repeatable Read."));
        var s1 = await a1.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var a2 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            new AdaptiveAnswerRequest(s1!.CurrentQuestion!.Id, "Concurrency trade-offs with serializable isolation."));
        var s2 = await a2.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.Equal("completed", s2!.Status);

        var originalProfile = s2.Profile;
        Assert.NotNull(originalProfile);
        var originalSqlLevel = originalProfile.Skills.First(s => s.SkillId == "sql").FinalLevel;

        // 2. Recommend and evaluate project
        await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/project/recommend", null);

        var submitReq = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://github.com/test/repo",
            ProjectSummary: "Built SQL transaction system",
            ImplementationExplanation: "Repeatable Read isolation with index scan verification",
            ArchitectureDecisions: "Documented trade-offs",
            TestingExplanation: "Automated test suite with concurrency simulation"
        );
        var evalRes = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/project/submit", submitReq);
        Assert.Equal(HttpStatusCode.OK, evalRes.StatusCode);

        // 3. Inspect profile again: diagnostic level MUST NOT have mutated!
        var profileRes = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/profile");
        Assert.Equal(HttpStatusCode.OK, profileRes.StatusCode);
        var recheckedProfile = await profileRes.Content.ReadFromJsonAsync<CareerReadinessProfile>();

        Assert.NotNull(recheckedProfile);
        var currentSqlLevel = recheckedProfile.Skills.First(s => s.SkillId == "sql").FinalLevel;
        Assert.Equal(originalSqlLevel, currentSqlLevel);
    }

    [Fact]
    public async Task Test12_NoNumericReadinessScores_Or_Percentages_InResponses()
    {
        var context = CreateSampleGapContext();
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);

        var json = JsonSerializer.Serialize(project);
        Assert.DoesNotMatch(@"\b\d{1,3}%", json);
        Assert.DoesNotContain("job-ready score", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("readiness score", json, StringComparison.OrdinalIgnoreCase);

        var submission = new SubmitProjectEvidenceRequest("url", "summary", "impl", "arch", "tests");
        var eval = await _deterministicEvaluator.EvaluateAsync(project, submission);
        var evalJson = JsonSerializer.Serialize(eval);

        Assert.DoesNotMatch(@"\b\d{1,3}%", evalJson);
        Assert.DoesNotContain("score", evalJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test13_Privacy_PublicResponses_HideRubrics_ExpectedSignals_SystemPrompts()
    {
        var context = CreateSampleGapContext();
        var project = await _deterministicRecommender.RecommendGapBasedAsync(context);
        var submission = new SubmitProjectEvidenceRequest("url", "summary", "impl", "arch", "tests");
        var eval = await _deterministicEvaluator.EvaluateAsync(project, submission);

        var evalJson = JsonSerializer.Serialize(eval);
        Assert.DoesNotContain("expectedSignals", evalJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rubric", evalJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("system prompt", evalJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test14_Legacy_FinancialAnalyst_And_Milestone5_Endpoints_Unchanged()
    {
        var request = new RecommendProjectRequest(
            RoleId: "financial-analyst",
            TopGaps: new List<string> { "Financial Modeling", "Forecasting", "Data Analysis" },
            Roadmap: new List<RoadmapInputItem>
            {
                new("Financial Modeling", 1, "Build 3-statement model"),
                new("Forecasting", 2, "3-year forecast")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProjectRecommendationResponse>();
        Assert.NotNull(result);
        Assert.Equal("Company Financial Health & 3-Year Outlook", result.Title);
        Assert.NotEmpty(result.Requirements);
        Assert.NotEmpty(result.ExpectedDeliverables!);
    }
}
