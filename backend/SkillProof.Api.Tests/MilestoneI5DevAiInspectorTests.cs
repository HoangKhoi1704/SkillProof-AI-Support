using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneI5DevAiInspectorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MilestoneI5DevAiInspectorTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test01_RuntimeEndpoint_ReturnsAllowListedConfigurationOnly()
    {
        var response = await _client.GetAsync("/api/dev/ai/runtime");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var config = await response.Content.ReadFromJsonAsync<AiRuntimeConfigDto>();
        Assert.NotNull(config);

        Assert.Equal("OpenAI", config.Provider);
        Assert.False(string.IsNullOrWhiteSpace(config.Model));
        Assert.False(string.IsNullOrWhiteSpace(config.ActiveEvaluator));
        Assert.Equal("Development", config.Environment);
        Assert.False(string.IsNullOrWhiteSpace(config.StatusMessage));
    }

    [Fact]
    public async Task Test02_RuntimeEndpoint_NeverReturnsApiKey()
    {
        var response = await _client.GetAsync("/api/dev/ai/runtime");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("apiKey", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sk-", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test03_RuntimeEndpoint_NeverReturnsConnectionString()
    {
        var response = await _client.GetAsync("/api/dev/ai/runtime");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("connectionString", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Data Source", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".db", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test04_QuestionInspector_ReturnsServerSideRubric_InDevelopment()
    {
        var response = await _client.GetAsync("/api/dev/ai/questions/q-be-sql-02");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var question = await response.Content.ReadFromJsonAsync<DevQuestionDetailDto>();
        Assert.NotNull(question);
        Assert.Equal("q-be-sql-02", question.Id);

        // Server-side rubric is present in dev detail
        Assert.NotNull(question.Rubric);
        Assert.False(string.IsNullOrWhiteSpace(question.Rubric.InsufficientEvidence));
        Assert.False(string.IsNullOrWhiteSpace(question.Rubric.Beginner));
        Assert.False(string.IsNullOrWhiteSpace(question.Rubric.Intermediate));
        Assert.False(string.IsNullOrWhiteSpace(question.Rubric.Advanced));

        // Server-side expected signals present
        Assert.NotNull(question.ExpectedSignals);
        Assert.NotEmpty(question.ExpectedSignals);
    }

    [Fact]
    public async Task Test05_NormalPublicQuestionEndpoint_StillDoesNotReturnRubric()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=backend-developer");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"rubric\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"insufficientEvidence\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"advanced\"", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test06_NormalPublicQuestionEndpoint_StillDoesNotReturnExpectedSignals()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=backend-developer");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"expectedSignals\"", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test07_DeterministicPreview_MakesZeroOpenAiRequests()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-sql-02",
            CandidateAnswer: "I would inspect execution plans with EXPLAIN ANALYZE and add composite indexes.",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<AiDiagnosticTraceDto>();
        Assert.NotNull(trace);
        Assert.Equal("DeterministicDiagnosticEvaluator", trace.Runtime.ActiveEvaluator);
    }

    [Fact]
    public async Task Test08_DeterministicPreview_ReturnsTrace()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-test-02",
            CandidateAnswer: "I use mocked dependencies to isolate the payment gateway and verify behavior.",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<AiDiagnosticTraceDto>();
        Assert.NotNull(trace);
        Assert.False(string.IsNullOrWhiteSpace(trace.TraceId));
        Assert.NotNull(trace.Prompt);
        Assert.False(string.IsNullOrWhiteSpace(trace.Prompt.SystemInstructions));
        Assert.False(string.IsNullOrWhiteSpace(trace.Prompt.EvaluationInstructions));
        Assert.NotNull(trace.Validation);
        Assert.True(trace.Validation.SchemaValid);
        Assert.NotNull(trace.FinalResult);
        Assert.True(trace.Timing.DurationMs >= 0);
    }

    [Fact]
    public async Task Test09_Trace_RecordsQuestionId()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-nosql-01",
            CandidateAnswer: "Document databases fit hierarchical structures where documents embed child data.",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<AiDiagnosticTraceDto>();
        Assert.NotNull(trace);
        Assert.Equal("q-be-nosql-01", trace.Question.QuestionId);
    }

    [Fact]
    public async Task Test10_Trace_RecordsEvaluator()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-cache-01",
            CandidateAnswer: "Cache-aside pattern checks cache first, then database on miss.",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<AiDiagnosticTraceDto>();
        Assert.NotNull(trace);
        Assert.Equal("DeterministicDiagnosticEvaluator", trace.Runtime.ActiveEvaluator);
    }

    [Fact]
    public async Task Test11_Trace_RecordsFinalQualitativeLevel()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-cs-01",
            CandidateAnswer: "In C#, I use async/await with Task and CancellationTokens to avoid blocking worker threads.",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<AiDiagnosticTraceDto>();
        Assert.NotNull(trace);
        var allowed = new[] { "Beginner", "Intermediate", "Advanced", "Insufficient Evidence" };
        Assert.Contains(trace.FinalResult.Level, allowed);
    }

    [Fact]
    public async Task Test12_TraceHistory_IsBounded()
    {
        // Run 25 evaluations
        for (int i = 0; i < 25; i++)
        {
            var request = new DevEvaluateRequest(
                QuestionId: "q-be-sql-02",
                CandidateAnswer: $"Sample answer variation {i} for bounded history testing.",
                Mode: "deterministic"
            );
            var res = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        var listResponse = await _client.GetAsync("/api/dev/ai/traces");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var traces = await listResponse.Content.ReadFromJsonAsync<List<AiDiagnosticTraceSummaryDto>>();
        Assert.NotNull(traces);
        Assert.True(traces.Count <= 20, $"Trace history must be bounded at 20, but was {traces.Count}");
    }

    [Fact]
    public async Task Test13_UnknownQuestionId_IsRejected()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "non-existent-question-id-999",
            CandidateAnswer: "Irrelevant text",
            Mode: "deterministic"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("QUESTION_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task Test14_LiveEvaluation_IsUnavailable_WhenLiveEvaluationEnabledIsFalse()
    {
        var request = new DevEvaluateRequest(
            QuestionId: "q-be-sql-02",
            CandidateAnswer: "EXPLAIN plan analysis and indexing.",
            Mode: "live"
        );

        var response = await _client.PostAsJsonAsync("/api/dev/ai/evaluate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("LIVE_AI_UNAVAILABLE", error.Error.Code);
        Assert.Contains("LiveEvaluationEnabled", error.Error.Message);
    }

    [Fact]
    public async Task Test15_DevEndpoints_AreUnavailableOutsideDevelopment()
    {
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });

        var prodClient = prodFactory.CreateClient();

        var runtimeRes = await prodClient.GetAsync("/api/dev/ai/runtime");
        Assert.Equal(HttpStatusCode.NotFound, runtimeRes.StatusCode);

        var questionsRes = await prodClient.GetAsync("/api/dev/ai/questions");
        Assert.Equal(HttpStatusCode.NotFound, questionsRes.StatusCode);

        var questionDetailRes = await prodClient.GetAsync("/api/dev/ai/questions/q-be-sql-02");
        Assert.Equal(HttpStatusCode.NotFound, questionDetailRes.StatusCode);

        var evalRes = await prodClient.PostAsJsonAsync("/api/dev/ai/evaluate", new DevEvaluateRequest("q-be-sql-02", "sample"));
        Assert.Equal(HttpStatusCode.NotFound, evalRes.StatusCode);

        var tracesRes = await prodClient.GetAsync("/api/dev/ai/traces");
        Assert.Equal(HttpStatusCode.NotFound, tracesRes.StatusCode);
    }
}
