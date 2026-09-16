using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class Milestone5ProjectTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DeterministicProjectRecommender _deterministicRecommender;
    private readonly OpenAiProjectRecommender _openAiRecommender;

    public Milestone5ProjectTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _deterministicRecommender = new DeterministicProjectRecommender();
        _openAiRecommender = new OpenAiProjectRecommender(
            apiKey: "dummy-key-for-unit-tests",
            model: "gpt-5.4-mini",
            fallbackRecommender: _deterministicRecommender,
            logger: NullLogger<OpenAiProjectRecommender>.Instance
        );
    }

    [Fact]
    public async Task PostProjectRecommend_WithBackendDeveloperGaps_Returns200AndValidProject()
    {
        var request = new RecommendProjectRequest(
            RoleId: "backend-developer",
            TopGaps: new List<string> { "SQL / Database", "Testing", "System Design" },
            Roadmap: new List<RoadmapInputItem>
            {
                new("SQL / Database", 1, "Master indexing strategies"),
                new("Testing", 2, "Learn unit testing fundamentals")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProjectRecommendationResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.NotEmpty(result.Requirements);

        // Every major requirement maps to a diagnosed top gap
        var diagnosedSet = new HashSet<string>(request.TopGaps, StringComparer.OrdinalIgnoreCase);
        foreach (var req in result.Requirements)
        {
            Assert.Contains(req.TargetsSkill, diagnosedSet);
            Assert.False(string.IsNullOrWhiteSpace(req.Requirement));
            Assert.False(string.IsNullOrWhiteSpace(req.Deliverable));
        }

        // Expected deliverables are observable
        Assert.NotNull(result.ExpectedDeliverables);
        Assert.NotEmpty(result.ExpectedDeliverables);
    }

    [Fact]
    public async Task PostProjectRecommend_WithFinancialAnalystGaps_Returns200AndValidProject()
    {
        var request = new RecommendProjectRequest(
            RoleId: "financial-analyst",
            TopGaps: new List<string> { "Financial Modeling", "Forecasting", "Data Analysis" }
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ProjectRecommendationResponse>();
        Assert.NotNull(result);
        Assert.Equal("Company Financial Health & 3-Year Outlook", result.Title);
        Assert.NotEmpty(result.Requirements);

        var diagnosedSet = new HashSet<string>(request.TopGaps, StringComparer.OrdinalIgnoreCase);
        foreach (var req in result.Requirements)
        {
            Assert.Contains(req.TargetsSkill, diagnosedSet);
            Assert.False(string.IsNullOrWhiteSpace(req.Requirement));
            Assert.False(string.IsNullOrWhiteSpace(req.Deliverable));
        }
    }

    [Fact]
    public async Task PostProjectRecommend_MissingRoleId_Returns400ValidationError()
    {
        var request = new RecommendProjectRequest(
            RoleId: "",
            TopGaps: new List<string> { "SQL / Database" }
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error?.Error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("roleId is required", error.Error.Message);
    }

    [Fact]
    public async Task PostProjectRecommend_InvalidRoleId_Returns400ValidationError()
    {
        var request = new RecommendProjectRequest(
            RoleId: "game-developer",
            TopGaps: new List<string> { "Graphics" }
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error?.Error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("must be one of: backend-developer, financial-analyst", error.Error.Message);
    }

    [Fact]
    public async Task PostProjectRecommend_EmptyTopGaps_Returns400ValidationError()
    {
        var request = new RecommendProjectRequest(
            RoleId: "backend-developer",
            TopGaps: new List<string>()
        );

        var response = await _client.PostAsJsonAsync("/api/projects/recommend", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error?.Error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("topGaps must contain at least 1", error.Error.Message);
    }

    [Fact]
    public void Validator_RejectsInventedSkillsAndPreservesOnlyDiagnosedGaps()
    {
        var rawJson = """
        {
          "title": "Unrelated Web App",
          "description": "A web project with invented skills",
          "reason": "Addresses gaps",
          "requirements": [
            {
              "requirement": "Build a React front-end",
              "targetsSkill": "Frontend / React",
              "deliverable": "React app"
            },
            {
              "requirement": "Write SQL queries and execution plans",
              "targetsSkill": "SQL / Database",
              "deliverable": "SQL migrations"
            }
          ],
          "expectedDeliverables": ["React app", "SQL migrations"]
        }
        """;

        var diagnosedGaps = new List<string> { "SQL / Database", "Testing" };
        var validated = _openAiRecommender.ValidateAndProcessAiResponse("backend-developer", rawJson, diagnosedGaps);

        Assert.NotNull(validated);
        // The invented skill "Frontend / React" must be dropped
        Assert.DoesNotContain(validated.Requirements, r => r.TargetsSkill == "Frontend / React");
        // Diagnosed gap "SQL / Database" is preserved
        Assert.Contains(validated.Requirements, r => r.TargetsSkill == "SQL / Database");
        // The missing diagnosed gap "Testing" was backfilled with core requirement
        Assert.Contains(validated.Requirements, r => r.TargetsSkill == "Testing");
    }

    [Fact]
    public void Validator_RejectsMalformedJsonAndReturnsNull()
    {
        var invalidJson = "{ invalid json content }";
        var result = _openAiRecommender.ValidateAndProcessAiResponse("backend-developer", invalidJson, new List<string> { "SQL / Database" });
        Assert.Null(result);
    }

    [Fact]
    public void Validator_SanitizesNumericPercentagesAndScores()
    {
        var rawJson = """
        {
          "title": "Expense Tracker",
          "description": "Achieve 95% test coverage and 100% database performance.",
          "reason": "Directly forces implementation with 85% confidence.",
          "requirements": [
            {
              "requirement": "Target 90% query speedup",
              "targetsSkill": "SQL / Database",
              "deliverable": "Execution plan"
            }
          ],
          "expectedDeliverables": ["Execution plan"]
        }
        """;

        var validated = _openAiRecommender.ValidateAndProcessAiResponse("backend-developer", rawJson, new List<string> { "SQL / Database" });
        Assert.NotNull(validated);
        Assert.DoesNotContain("95%", validated.Description);
        Assert.DoesNotContain("100%", validated.Description);
        Assert.DoesNotContain("85%", validated.Reason);
        Assert.DoesNotContain("90%", validated.Requirements[0].Requirement);
    }

    [Fact]
    public async Task Fallback_ProducesValidRoleAppropriateProject()
    {
        var backendProject = await _deterministicRecommender.RecommendAsync(new RecommendProjectRequest(
            "backend-developer",
            new List<string> { "SQL / Database", "Testing", "System Design" }
        ));

        Assert.Equal("Expense Management API", backendProject.Title);
        Assert.Equal(3, backendProject.Requirements.Count);
        Assert.Contains(backendProject.Requirements, r => r.TargetsSkill == "SQL / Database");
        Assert.Contains(backendProject.Requirements, r => r.TargetsSkill == "Testing");
        Assert.Contains(backendProject.Requirements, r => r.TargetsSkill == "System Design");

        var financialProject = await _deterministicRecommender.RecommendAsync(new RecommendProjectRequest(
            "financial-analyst",
            new List<string> { "Financial Modeling", "Forecasting" }
        ));

        Assert.Equal("Company Financial Health & 3-Year Outlook", financialProject.Title);
        Assert.Equal(2, financialProject.Requirements.Count);
        Assert.Contains(financialProject.Requirements, r => r.TargetsSkill == "Financial Modeling");
        Assert.Contains(financialProject.Requirements, r => r.TargetsSkill == "Forecasting");
    }
}
