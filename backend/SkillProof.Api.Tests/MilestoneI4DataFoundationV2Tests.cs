using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class MilestoneI4DataFoundationV2Tests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
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

    private static readonly string[] AllEightLanguages =
    [
        "csharp",
        "java",
        "python",
        "cpp",
        "javascript",
        "typescript",
        "go",
        "rust"
    ];

    public MilestoneI4DataFoundationV2Tests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SQLite_ContainsExactly48Questions_AndCorrectCategoryBreakdown()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var totalQuestions = await db.Questions.CountAsync();
        Assert.Equal(48, totalQuestions);

        var totalRubrics = await db.Rubrics.CountAsync();
        Assert.Equal(48, totalRubrics);

        var coreCount = await db.Questions.CountAsync(q => AllSixCoreCompetencyIds.Contains(q.SkillId));
        Assert.Equal(18, coreCount);

        var nosqlCount = await db.Questions.CountAsync(q => q.SkillId == "nosql");
        Assert.Equal(3, nosqlCount);

        var cachingCount = await db.Questions.CountAsync(q => q.SkillId == "caching");
        Assert.Equal(3, cachingCount);

        foreach (var lang in AllEightLanguages)
        {
            var langCount = await db.Questions.CountAsync(q => q.SkillId == lang);
            Assert.Equal(3, langCount);
        }

        // Verify rubric completeness: every rubric must have all 4 qualitative descriptions
        var rubrics = await db.Rubrics.ToListAsync();
        Assert.All(rubrics, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.InsufficientEvidence));
            Assert.False(string.IsNullOrWhiteSpace(r.Beginner));
            Assert.False(string.IsNullOrWhiteSpace(r.Intermediate));
            Assert.False(string.IsNullOrWhiteSpace(r.Advanced));
        });

        // Verify QuestionSubskills
        var qsCount = await db.QuestionSubskills.CountAsync();
        Assert.True(qsCount >= 48);

        // Verify QuestionFrameworkSources
        var qfsCount = await db.QuestionFrameworkSources.CountAsync();
        Assert.True(qfsCount >= 48);

        // Verify QuestionInterviewEvidence
        var qieCount = await db.QuestionInterviewEvidence.CountAsync();
        Assert.Equal(2, qieCount);
    }

    [Fact]
    public async Task SQLite_SeedingIsDeterministicAndIdempotent()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Re-run seeder explicitly
        await seeder.SeedAsync();

        var qCount = await db.Questions.CountAsync();
        var rCount = await db.Rubrics.CountAsync();
        var sCount = await db.Sources.CountAsync();
        var skCount = await db.Skills.CountAsync();

        Assert.Equal(48, qCount);
        Assert.Equal(48, rCount);
        Assert.Equal(42, sCount);
        Assert.Equal(22, skCount);
    }

    [Fact]
    public async Task FrozenCoreBaseline_UnchangedInSQLite()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var frozenMap = new Dictionary<string, (string SkillId, string Difficulty)>
        {
            ["q-be-prog-01"] = ("programming-fundamentals", "foundation"),
            ["q-be-prog-02"] = ("programming-fundamentals", "applied"),
            ["q-be-prog-03"] = ("programming-fundamentals", "advanced-reasoning"),
            ["q-be-rest-01"] = ("rest-api", "foundation"),
            ["q-be-rest-02"] = ("rest-api", "applied"),
            ["q-be-rest-03"] = ("rest-api", "advanced-reasoning"),
            ["q-be-sql-01"] = ("sql", "foundation"),
            ["q-be-sql-02"] = ("sql", "applied"),
            ["q-be-sql-03"] = ("sql", "advanced-reasoning"),
            ["q-be-test-01"] = ("testing", "foundation"),
            ["q-be-test-02"] = ("testing", "applied"),
            ["q-be-test-03"] = ("testing", "advanced-reasoning"),
            ["q-be-auth-01"] = ("authentication-security", "foundation"),
            ["q-be-auth-02"] = ("authentication-security", "applied"),
            ["q-be-auth-03"] = ("authentication-security", "advanced-reasoning"),
            ["q-be-sys-01"] = ("system-design", "foundation"),
            ["q-be-sys-02"] = ("system-design", "applied"),
            ["q-be-sys-03"] = ("system-design", "advanced-reasoning")
        };

        foreach (var (qId, expected) in frozenMap)
        {
            var q = await db.Questions.FirstOrDefaultAsync(x => x.Id == qId);
            Assert.NotNull(q);
            Assert.Equal(expected.SkillId, q.SkillId);
            Assert.Equal(expected.Difficulty, q.Difficulty);
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionText));
        }
    }

    [Theory]
    [InlineData("nosql", "q-be-nosql-02")]
    [InlineData("caching", "q-be-cache-02")]
    public async Task SelectQuestions_RecommendedCompetency_ReturnsAppliedQuestion(string competencyId, string expectedQuestionId)
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { competencyId },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Single(result.Questions);

        var q = result.Questions[0];
        Assert.Equal(expectedQuestionId, q.Id);
        Assert.Equal(competencyId, q.CompetencyId);
        Assert.Equal("applied", q.Difficulty);

        Assert.Equal(new[] { competencyId }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Empty(result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Theory]
    [InlineData("csharp", "q-be-cs-02")]
    [InlineData("java", "q-be-java-02")]
    [InlineData("python", "q-be-py-02")]
    [InlineData("cpp", "q-be-cpp-02")]
    [InlineData("javascript", "q-be-js-02")]
    [InlineData("typescript", "q-be-ts-02")]
    [InlineData("go", "q-be-go-02")]
    [InlineData("rust", "q-be-rust-02")]
    public async Task SelectQuestions_PrimaryLanguage_ReturnsAppliedLanguageQuestion(string languageId, string expectedQuestionId)
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql" },
            languageId
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Questions.Count);

        var langQ = result.Questions.FirstOrDefault(q => q.CompetencyId == languageId);
        Assert.NotNull(langQ);
        Assert.Equal(expectedQuestionId, langQ.Id);
        Assert.Equal("applied", langQ.Difficulty);

        Assert.Equal(languageId, result.AssessmentCoverage.AssessedPrimaryLanguageId);
    }

    [Fact]
    public async Task SelectQuestions_RecommendedMode_WithCSharp_ProducesSevenQuestions()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            AllSixCoreCompetencyIds.ToList(),
            "csharp"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(7, result.Questions.Count);

        // First 6 are Core applied questions
        for (int i = 0; i < 6; i++)
        {
            Assert.Contains(result.Questions[i].CompetencyId, AllSixCoreCompetencyIds);
            Assert.Equal("applied", result.Questions[i].Difficulty);
        }

        // 7th question is C# applied question
        var lastQ = result.Questions[6];
        Assert.Equal("csharp", lastQ.CompetencyId);
        Assert.Equal("q-be-cs-02", lastQ.Id);
        Assert.Equal("applied", lastQ.Difficulty);

        Assert.Equal(6, result.AssessmentCoverage.AssessedSkillIds.Count);
        Assert.Equal("csharp", result.AssessmentCoverage.AssessedPrimaryLanguageId);
        Assert.Empty(result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Fact]
    public async Task SelectQuestions_CustomMode_SqlTestingPython_ProducesThreeQuestions()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql", "testing" },
            "python"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Questions.Count);

        Assert.Equal("q-be-sql-02", result.Questions[0].Id);
        Assert.Equal("q-be-test-02", result.Questions[1].Id);
        Assert.Equal("q-be-py-02", result.Questions[2].Id);

        Assert.Equal(new[] { "sql", "testing" }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Equal("python", result.AssessmentCoverage.AssessedPrimaryLanguageId);
    }

    [Fact]
    public async Task SelectQuestions_CustomMode_NoSqlCachingGo_ProducesThreeQuestions()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "nosql", "caching" },
            "go"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Questions.Count);

        Assert.Equal("q-be-nosql-02", result.Questions[0].Id);
        Assert.Equal("q-be-cache-02", result.Questions[1].Id);
        Assert.Equal("q-be-go-02", result.Questions[2].Id);

        Assert.Equal(new[] { "nosql", "caching" }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Equal("go", result.AssessmentCoverage.AssessedPrimaryLanguageId);
    }

    [Fact]
    public async Task SelectQuestions_OptionalDocker_RemainsUnsupportedAndNotInvented()
    {
        var request = new DynamicQuestionSelectionRequest(
            "backend-developer",
            new List<string> { "sql", "docker" },
            null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(result);

        // Only SQL question returned; no synthetic docker question
        Assert.Single(result.Questions);
        Assert.Equal("sql", result.Questions[0].CompetencyId);

        Assert.Equal(new[] { "sql" }, result.AssessmentCoverage.AssessedSkillIds);
        Assert.Equal(new[] { "docker" }, result.AssessmentCoverage.UnsupportedSkillIds);
    }

    [Fact]
    public async Task EvaluateDiagnostic_AcceptsNewQuestionIds_AndEvaluatesDeterministically()
    {
        var evalRequest = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new("q-be-nosql-02", "I would use MongoDB document store collections with embedding for order items to ensure single-document atomic updates."),
                new("q-be-cache-02", "I would implement cache-aside using Redis with distributed locks (SET NX EX) to prevent cache stampedes under high concurrency."),
                new("q-be-go-02", "I would implement a worker pool using buffered channels and sync.WaitGroup with context.Context cancellation to prevent goroutine leaks.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", evalRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal(3, result.Skills.Count);

        var nosqlSkill = result.Skills.FirstOrDefault(s => s.Name == "NoSQL Databases");
        Assert.NotNull(nosqlSkill);
        Assert.Equal("Intermediate", nosqlSkill.Level);

        var cacheSkill = result.Skills.FirstOrDefault(s => s.Name == "Caching & Performance");
        Assert.NotNull(cacheSkill);
        Assert.Equal("Intermediate", cacheSkill.Level);

        var goSkill = result.Skills.FirstOrDefault(s => s.Name == "Go");
        Assert.NotNull(goSkill);
        Assert.Equal("Intermediate", goSkill.Level);
    }

    [Fact]
    public async Task FinancialAnalyst_QuestionsEndpoint_RemainsUnchangedAndBackwardCompatible()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=financial-analyst");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var questions = await response.Content.ReadFromJsonAsync<List<DiagnosticQuestionDto>>();
        Assert.NotNull(questions);
        Assert.Equal(6, questions.Count);
        Assert.All(questions, q => Assert.False(string.IsNullOrWhiteSpace(q.QuestionText)));
    }
}
