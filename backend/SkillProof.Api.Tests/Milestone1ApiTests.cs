using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class Milestone1ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public Milestone1ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoles_ReturnsOk_WithTwoPrototypeRoles()
    {
        var response = await _client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>();
        Assert.NotNull(roles);
        Assert.Equal(2, roles.Count);

        var beRole = roles.FirstOrDefault(r => r.Id == "backend-developer");
        Assert.NotNull(beRole);
        Assert.Equal("Backend Developer", beRole.Name);
        Assert.False(string.IsNullOrWhiteSpace(beRole.Description));

        var faRole = roles.FirstOrDefault(r => r.Id == "financial-analyst");
        Assert.NotNull(faRole);
        Assert.Equal("Financial Analyst", faRole.Name);
        Assert.False(string.IsNullOrWhiteSpace(faRole.Description));
    }

    [Fact]
    public async Task GetQuestions_BackendDeveloper_Returns6Questions_WithoutRubrics()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=backend-developer");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        // Invariant: Public questions must never contain the word "rubric"
        Assert.DoesNotContain("\"rubric\"", rawJson, StringComparison.OrdinalIgnoreCase);

        var questions = await response.Content.ReadFromJsonAsync<List<DiagnosticQuestionDto>>();
        Assert.NotNull(questions);
        Assert.Equal(6, questions.Count);

        // Verify integer IDs 1..6
        for (int i = 1; i <= 6; i++)
        {
            var q = questions.FirstOrDefault(item => item.Id == i);
            Assert.NotNull(q);
            Assert.Equal("backend-developer", q.CareerRoleId);
            Assert.False(string.IsNullOrWhiteSpace(q.Competency));
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionText));
            Assert.False(string.IsNullOrWhiteSpace(q.Type));
            Assert.Equal("team_curated", q.SourceType);
        }
    }

    [Fact]
    public async Task GetQuestions_FinancialAnalyst_Returns6Questions_WithoutRubrics()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=financial-analyst");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        // Invariant: Public questions must never contain the word "rubric"
        Assert.DoesNotContain("\"rubric\"", rawJson, StringComparison.OrdinalIgnoreCase);

        var questions = await response.Content.ReadFromJsonAsync<List<DiagnosticQuestionDto>>();
        Assert.NotNull(questions);
        Assert.Equal(6, questions.Count);

        // Verify integer IDs 7..12
        for (int i = 7; i <= 12; i++)
        {
            var q = questions.FirstOrDefault(item => item.Id == i);
            Assert.NotNull(q);
            Assert.Equal("financial-analyst", q.CareerRoleId);
            Assert.False(string.IsNullOrWhiteSpace(q.Competency));
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionText));
            Assert.False(string.IsNullOrWhiteSpace(q.Type));
            Assert.Equal("team_curated", q.SourceType);
        }
    }

    [Fact]
    public async Task GetQuestions_MissingRoleId_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
    }

    [Fact]
    public async Task GetQuestions_InvalidRoleId_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=machine-learning-engineer");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("backend-developer", error.Error.Message);
    }
}
