using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class Milestone2ApiTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly HashSet<string> AllowedQualitativeLevels = new()
    {
        "Beginner",
        "Intermediate",
        "Advanced",
        "Insufficient Evidence"
    };

    public Milestone2ApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Evaluate_BackendDeveloper_ValidAnswers_Returns200Ok_WithCalibratedProfileAndTopGaps()
    {
        var request = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new(1, "PUT replaces the entire resource representation while PATCH performs a partial update with idempotency guarantees."),
                new(2, "I would inspect the execution plan using EXPLAIN and add a composite index on CustomerId and OrderDate."),
                new(3, "I would use mock interfaces to isolate payment gateway calls and test success and error status codes."),
                new(4, "I would use Redis with a sliding window counter and return HTTP 429 Too Many Requests."),
                new(5, "Stateless JWT verifies cryptographic signature on the header and payload claims using HMAC or RSA."),
                new(6, "I would stream records with IAsyncEnumerable and pass a CancellationToken to allow graceful cancellation.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal(6, result.Skills.Count);

        // Verify only approved levels
        foreach (var skill in result.Skills)
        {
            Assert.Contains(skill.Level, AllowedQualitativeLevels);
            Assert.False(string.IsNullOrWhiteSpace(skill.Name));
            Assert.False(string.IsNullOrWhiteSpace(skill.Reason));
        }

        // Verify top gaps: maximum 3, matches Section 14
        Assert.NotEmpty(result.TopGaps);
        Assert.True(result.TopGaps.Count <= 3);
        Assert.Contains("SQL / Database", result.TopGaps);
        Assert.Contains("Testing", result.TopGaps);
        Assert.Contains("System Design", result.TopGaps);
    }

    [Fact]
    public async Task Evaluate_FinancialAnalyst_ValidAnswers_Returns200Ok_WithCalibratedProfileAndTopGaps()
    {
        var request = new EvaluationRequest(
            "financial-analyst",
            new List<DiagnosticAnswerSubmission>
            {
                new(7, "Net Income flows to the top of the Cash Flow Statement under Operating Cash Flow and increases Retained Earnings on the Balance Sheet."),
                new(8, "I would use XLOOKUP or INDEX/MATCH with IFERROR wrappers and structured tables to build a dynamic audit-ready dashboard."),
                new(9, "I would build a 3-statement model linked by Net Income and Cash, with SaaS drivers like MRR, Churn, and CAC."),
                new(10, "I would analyze working capital buildup in Accounts Receivable or Inventory causing negative operating cash flow despite revenue growth."),
                new(11, "I would compute Current Ratio and Quick Ratio. A divergence signals excess cash tied up in illiquid inventory."),
                new(12, "I would use a variance waterfall decomposing price, volume, product mix shift, and fixed cost inflation.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);
        Assert.Equal("financial-analyst", result.RoleId);
        Assert.Equal(6, result.Skills.Count);

        foreach (var skill in result.Skills)
        {
            Assert.Contains(skill.Level, AllowedQualitativeLevels);
        }

        Assert.NotEmpty(result.TopGaps);
        Assert.True(result.TopGaps.Count <= 3);
        Assert.Contains("Financial Modeling", result.TopGaps);
        Assert.Contains("Forecasting", result.TopGaps);
        Assert.Contains("Data Analysis", result.TopGaps);
    }

    [Fact]
    public async Task Evaluate_EmptyAnswers_ProducesInsufficientEvidence()
    {
        var request = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new(1, ""), // Empty
                new(2, "   "), // Whitespace
                new(3, "idk"), // Too short / unusable
                new(4, "n/a"),
                new(5, "none"),
                new(6, "")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);

        foreach (var skill in result.Skills)
        {
            Assert.Equal("Insufficient Evidence", skill.Level);
            Assert.Empty(skill.Evidence);
            Assert.Contains("lacks sufficient", skill.Reason, StringComparison.OrdinalIgnoreCase);
        }

        // Top gaps are populated from Insufficient Evidence
        Assert.NotEmpty(result.TopGaps);
        Assert.True(result.TopGaps.Count <= 3);
    }

    [Fact]
    public async Task Evaluate_InvalidRoleId_Returns400BadRequest()
    {
        var request = new EvaluationRequest(
            "cybersecurity-specialist",
            new List<DiagnosticAnswerSubmission>
            {
                new(1, "Some answer here to test role validation")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
    }

    [Fact]
    public async Task Evaluate_EmptyAnswersList_Returns400BadRequest()
    {
        var request = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>()
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
    }

    [Fact]
    public async Task Evaluate_MismatchedQuestionId_Returns400BadRequest()
    {
        // Question ID 7 belongs to financial-analyst, not backend-developer
        var request = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new(7, "This question does not belong to backend developer role.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
    }
}
