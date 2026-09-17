using System.Net;
using System.Net.Http.Json;
using SkillProof.Api.Models;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV25ProjectMatcherTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MilestoneV25ProjectMatcherTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateCompletedSessionAsync(string roleId)
    {
        var selectedSkills = roleId switch
        {
            "backend-developer" => new List<string> { "sql", "testing" },
            "frontend-developer" => new List<string> { "frontend.html-core", "shared.javascript" },
            "data-analyst" => new List<string> { "data-analyst.spreadsheets", "shared.sql" },
            _ => new List<string> { "sql" }
        };

        var startPayload = new AdaptiveSessionRequest(
            RoleId: roleId,
            SelectedSkillIds: selectedSkills,
            PrimaryLanguageId: roleId == "backend-developer" ? "csharp" : null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startPayload);
        Assert.Equal(HttpStatusCode.OK, startRes.StatusCode);
        var startBody = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(startBody);

        var sessionId = startBody.SessionId;
        var currentQuestion = startBody.CurrentQuestion;

        while (currentQuestion != null)
        {
            var answerPayload = new AdaptiveAnswerRequest(
                currentQuestion.Id,
                "Demonstrated production-grade implementation with comprehensive error handling, modular architecture, and unit test coverage."
            );

            var ansRes = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", answerPayload);
            Assert.Equal(HttpStatusCode.OK, ansRes.StatusCode);
            var ansBody = await ansRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
            Assert.NotNull(ansBody);

            if (ansBody.Status == "completed")
            {
                break;
            }
            currentQuestion = ansBody.CurrentQuestion;
        }

        return sessionId;
    }

    [Fact]
    public async Task CuratedProjects_FrontendSession_ReturnsCategorizedPracticeAndPortfolioProjects()
    {
        var sessionId = await CreateCompletedSessionAsync("frontend-developer");

        // Act: GET /api/diagnostics/adaptive/sessions/{sessionId}/projects
        var res = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/projects");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<CuratedProjectRecommendationsResponse>();
        Assert.NotNull(body);
        Assert.Equal("frontend-developer", body.RoleId);

        // Verify Practice projects
        Assert.NotEmpty(body.PracticeProjects);
        Assert.All(body.PracticeProjects, p => Assert.Equal("practice", p.ProjectType));
        Assert.Contains(body.PracticeProjects, p => p.Id == "proj-fe-weather-practice" || p.Id == "proj-fe-todo-practice");

        // Verify Portfolio projects
        Assert.NotEmpty(body.PortfolioProjects);
        Assert.All(body.PortfolioProjects, p => Assert.Equal("portfolio", p.ProjectType));
        Assert.Contains(body.PortfolioProjects, p => p.Id == "proj-fe-platform-portfolio");

        // Verify deliverables and evidence requirements are populated
        var portfolioProj = body.PortfolioProjects.First();
        Assert.NotEmpty(portfolioProj.Deliverables);
        Assert.NotEmpty(portfolioProj.EvidenceRequirements);
        Assert.False(string.IsNullOrWhiteSpace(portfolioProj.WhyRecommended));
    }

    [Fact]
    public async Task CuratedProjects_BackendSession_ReturnsBackendEngineeredProjects()
    {
        var sessionId = await CreateCompletedSessionAsync("backend-developer");

        var res = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/projects");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<CuratedProjectRecommendationsResponse>();
        Assert.NotNull(body);
        Assert.Equal("backend-developer", body.RoleId);

        // Verify backend practice and portfolio projects
        Assert.Contains(body.PracticeProjects, p => p.Id == "proj-be-database-practice" || p.Id == "proj-be-cache-practice");
        Assert.Contains(body.PortfolioProjects, p => p.Id == "proj-be-resilient-portfolio");

        var proj = body.PortfolioProjects.First(p => p.Id == "proj-be-resilient-portfolio");
        Assert.Contains("backend.relational-databases", proj.CanonicalSkillIds);
        Assert.Contains("backend.testing", proj.CanonicalSkillIds);
    }

    [Fact]
    public async Task CuratedProjects_DataAnalystSession_ReturnsAnalystAppropriateProjects_WithoutMandatoryDeepLearning()
    {
        var sessionId = await CreateCompletedSessionAsync("data-analyst");

        var res = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/projects");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<CuratedProjectRecommendationsResponse>();
        Assert.NotNull(body);
        Assert.Equal("data-analyst", body.RoleId);

        // Verify analyst practice projects
        Assert.Contains(body.PracticeProjects, p => p.Id == "proj-da-eda-practice" || p.Id == "proj-da-sql-practice");

        // Verify analyst portfolio project
        Assert.Contains(body.PortfolioProjects, p => p.Id == "proj-da-bi-portfolio");

        // Ensure Deep Learning is NEVER a mandatory requirement for DA portfolio project
        foreach (var p in body.PortfolioProjects)
        {
            Assert.DoesNotContain("data-analyst.deep-learning", p.CanonicalSkillIds);
        }

        // Verify honest SkillProof provenance attribution
        var daPortfolio = body.PortfolioProjects.First(p => p.Id == "proj-da-bi-portfolio");
        Assert.Equal("skillproof-curated", daPortfolio.Provenance);
        Assert.Equal("skillproof-curated", daPortfolio.Source);
    }

    [Fact]
    public async Task CuratedProjects_TrustBoundary_UsesServerSideSession_RejectsInvalidSession()
    {
        // Act: Invalid session ID
        var res = await _client.GetAsync("/api/diagnostics/adaptive/sessions/fake-session-999/projects");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task SelectCuratedProject_SeamlesslyPreparesEvidenceSubmissionFlow()
    {
        var sessionId = await CreateCompletedSessionAsync("backend-developer");

        // Select the curated portfolio project
        var selectRes = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/projects/proj-be-resilient-portfolio/select", null);
        Assert.Equal(HttpStatusCode.OK, selectRes.StatusCode);

        var project = await selectRes.Content.ReadFromJsonAsync<GapBasedProjectDto>();
        Assert.NotNull(project);
        Assert.Equal("proj-be-resilient-portfolio", project.ProjectId);
        Assert.NotEmpty(project.Requirements);
        Assert.NotEmpty(project.Deliverables);

        // Submit evidence for this curated project to verify portfolio compatibility
        var evidencePayload = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://github.com/skillproof-user/order-inventory-service",
            ProjectSummary: "Production order service with PostgreSQL composite indexing and unit tests.",
            ImplementationExplanation: "Implemented clean architecture with repository pattern and transactional units of work.",
            ArchitectureDecisions: "Selected PostgreSQL READ COMMITTED with optimistic concurrency control via ETags.",
            TestingExplanation: "Written xUnit test suite isolating boundaries with Moq.",
            EvidenceExcerpts: new List<string> { "public async Task<Order> CreateOrderAsync(...) { ... }" }
        );

        var submitRes = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/project/submit", evidencePayload);
        Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);

        var eval = await submitRes.Content.ReadFromJsonAsync<ProjectEvaluationDto>();
        Assert.NotNull(eval);
        Assert.False(string.IsNullOrWhiteSpace(eval.OverallStatus));
    }
}
