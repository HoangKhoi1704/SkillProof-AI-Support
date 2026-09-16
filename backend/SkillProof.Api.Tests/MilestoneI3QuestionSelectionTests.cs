using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class MilestoneI3QuestionSelectionTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly string[] AllSixCoreCompetencyIds =
    [
        "programming-fundamentals",
        "rest-api",
        "sql",
        "testing",
        "authentication-security",
        "system-design"
    ];

    public MilestoneI3QuestionSelectionTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SelectQuestions_SqlOnly_ReturnsExactlyOneAppliedSqlQuestion()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Single(result.Questions);

        var question = result.Questions[0];
        Assert.Equal("q-be-sql-02", question.Id);
        Assert.Equal("sql", question.CompetencyId);
        Assert.Equal("applied", question.Difficulty);
        Assert.False(string.IsNullOrWhiteSpace(question.QuestionText));

        // Verify assessment coverage
        Assert.NotNull(result.AssessmentCoverage);
        Assert.Equal(new[] { "sql" }, result.AssessmentCoverage.RequestedSkillIds);
        Assert.Equal(new[] { "sql" }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Empty(result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Fact]
    public async Task SelectQuestions_SqlAndTesting_ReturnsExactlyTwoAppliedQuestions()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql", "testing" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Questions.Count);

        var sqlQ = result.Questions.FirstOrDefault(q => q.CompetencyId == "sql");
        var testQ = result.Questions.FirstOrDefault(q => q.CompetencyId == "testing");

        Assert.NotNull(sqlQ);
        Assert.Equal("applied", sqlQ.Difficulty);
        Assert.Equal("q-be-sql-02", sqlQ.Id);

        Assert.NotNull(testQ);
        Assert.Equal("applied", testQ.Difficulty);
        Assert.Equal("q-be-test-02", testQ.Id);

        Assert.Equal(2, result.AssessmentCoverage.AssessedSkillIds.Count);
        Assert.Empty(result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Fact]
    public async Task SelectQuestions_AllSixCore_ReturnsExactlySixAppliedQuestions()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            AllSixCoreCompetencyIds.ToList(),
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(6, result.Questions.Count);

        foreach (var q in result.Questions)
        {
            Assert.Equal("applied", q.Difficulty);
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionText));
            Assert.Contains(q.CompetencyId, AllSixCoreCompetencyIds);
        }

        Assert.Equal(6, result.AssessmentCoverage.AssessedSkillIds.Count);
        Assert.Empty(result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Fact]
    public async Task SelectQuestions_Deterministic_SameRequestReturnsIdenticalQuestionIdsAndOrder()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "testing", "programming-fundamentals", "sql" },
            null
        );

        var response1 = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        var response2 = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var result1 = await response1.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        var result2 = await response2.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.Questions.Count, result2.Questions.Count);

        for (int i = 0; i < result1.Questions.Count; i++)
        {
            Assert.Equal(result1.Questions[i].Id, result2.Questions[i].Id);
            Assert.Equal(result1.Questions[i].CompetencyId, result2.Questions[i].CompetencyId);
            Assert.Equal(result1.Questions[i].Difficulty, result2.Questions[i].Difficulty);
        }
    }

    [Fact]
    public async Task SelectQuestions_UnknownSkill_ReturnsBadRequestValidationError()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "quantum-computing-nonexistent" },
            "csharp"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("does not exist in catalog", error.Error.Message);
    }

    [Fact]
    public async Task SelectQuestions_InvalidRole_ReturnsNotFound()
    {
        var request = new DynamicQuestionSelectionRequest(
            "non-existent-role",
            new List<string> { "sql" },
            "csharp"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task SelectQuestions_LanguagePassedAsCompetency_ReturnsBadRequest()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "csharp" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error.Code);
        Assert.Contains("programming language and cannot be selected as an engineering competency", error.Error.Message);
    }

    [Fact]
    public async Task SelectQuestions_PrimaryLanguageId_ValidatesCorrectly()
    {
        // 1. Invalid primary language: a competency ID passed as language
        var requestWithCompetencyAsLang = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql" },
            "sql" // not a language
        );

        var response1 = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", requestWithCompetencyAsLang);
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);

        var error1 = await response1.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error1);
        Assert.Contains("not a valid programming language", error1.Error.Message);

        // 2. Non-existent primary language
        var requestWithUnknownLang = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql" },
            "brainfuck"
        );

        var response2 = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", requestWithUnknownLang);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);

        // 3. Valid primary language
        var validRequest = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql" },
            "rust"
        );

        var response3 = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", validRequest);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
    }

    [Fact]
    public async Task SelectQuestions_UnsupportedCatalogSkills_ReportedInCoverageAndNotInvented()
    {
        // "docker" and "git" are optional catalog skills with no question coverage in MVP
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql", "docker", "git" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);

        // Exactly 1 question returned for SQL
        Assert.Single(result.Questions);
        Assert.Equal("sql", result.Questions[0].CompetencyId);

        // Unsupported skills explicitly reported
        Assert.Equal(new[] { "sql" }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Contains("docker", result.AssessmentCoverage.UnsupportedSkillIds);
        Assert.Contains("git", result.AssessmentCoverage.UnsupportedSkillIds);
        Assert.Equal(2, result.AssessmentCoverage.UnsupportedSkillIds.Count);
    }

    [Fact]
    public async Task SelectQuestions_DoesNotExposeRubricsOrExpectedSignals()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql", "testing" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();

        // Rubrics, expected signals, and private evaluation guidance MUST NEVER be in the response
        Assert.DoesNotContain("\"rubric\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"expectedSignals\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"evidenceNotes\"", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"scoringGuidance\"", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateDiagnostic_WithDynamicSqliteQuestionIds_EvaluatesSuccessfully()
    {
        var evalRequest = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new("q-be-prog-02", "I would use a thread-safe ConcurrentDictionary or SemaphoreSlim with async/await, avoiding sync-over-async like Task.Result which causes deadlocks."),
                new("q-be-sql-02", "I would inspect the execution plan using EXPLAIN, identify full table scans, create composite indexing on customer_id and created_at, and eliminate N+1 queries using JOINs.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", evalRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal(2, result.Skills.Count);

        var progSkill = result.Skills.FirstOrDefault(s => s.Name.Contains("Programming", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(progSkill);
        Assert.Contains(progSkill.Level, new[] { "Beginner", "Intermediate", "Advanced" });

        var sqlSkill = result.Skills.FirstOrDefault(s => s.Name.Contains("SQL", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(sqlSkill);
        Assert.Contains(sqlSkill.Level, new[] { "Beginner", "Intermediate", "Advanced" });

        Assert.NotNull(result.TopGaps);
        Assert.True(result.TopGaps.Count <= 3);
    }

    [Fact]
    public async Task LegacyFinancialAnalyst_QuestionsEndpoint_RemainsUnchanged()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=financial-analyst");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var questions = await response.Content.ReadFromJsonAsync<List<DiagnosticQuestionDto>>();
        Assert.NotNull(questions);
        Assert.Equal(6, questions.Count);
        Assert.All(questions, q => Assert.False(string.IsNullOrWhiteSpace(q.QuestionText)));
    }
}
