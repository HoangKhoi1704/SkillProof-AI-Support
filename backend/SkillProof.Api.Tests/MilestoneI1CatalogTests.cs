using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class MilestoneI1CatalogTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly string[] ExpectedCoreIds =
    [
        "programming-fundamentals",
        "rest-api",
        "sql",
        "testing",
        "authentication-security",
        "system-design"
    ];

    private static readonly string[] ExpectedRecommendedIds =
    [
        "nosql",
        "caching"
    ];

    private static readonly string[] ExpectedOptionalIds =
    [
        "git",
        "docker",
        "cicd",
        "concurrency",
        "messaging",
        "observability"
    ];

    private static readonly string[] ExpectedLanguageIds =
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

    public MilestoneI1CatalogTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRoleSkills_BackendDeveloper_ExistsAndReturnsAllTaxonomyCategories()
    {
        var response = await _client.GetAsync("/api/roles/backend-developer/skills");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoleSkillsResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal("Backend Developer", result.RoleTitle);

        // Exact category counts
        Assert.Equal(6, result.Core.Count);
        Assert.Equal(2, result.Recommended.Count);
        Assert.Equal(6, result.Optional.Count);
        Assert.Equal(8, result.Languages.Count);

        // Verify Core competencies
        foreach (var core in result.Core)
        {
            Assert.Equal("core", core.Category);
            Assert.Equal("competency", core.SkillType);
            Assert.NotEmpty(core.Subskills);
        }

        // Verify Recommended competencies
        foreach (var rec in result.Recommended)
        {
            Assert.Equal("recommended", rec.Category);
            Assert.Equal("competency", rec.SkillType);
            Assert.NotEmpty(rec.Subskills);
        }

        // Verify Optional competencies
        foreach (var opt in result.Optional)
        {
            Assert.Equal("optional", opt.Category);
            Assert.Equal("competency", opt.SkillType);
            Assert.NotEmpty(opt.Subskills);
        }

        // Verify Languages (separated)
        foreach (var lang in result.Languages)
        {
            Assert.Equal("language", lang.Category);
            Assert.Equal("language", lang.SkillType);
            Assert.NotEmpty(lang.Subskills);
        }
    }

    [Fact]
    public async Task GetRoleSkills_StableIds_MatchFrozenDataset()
    {
        var response = await _client.GetAsync("/api/roles/backend-developer/skills");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoleSkillsResponse>();
        Assert.NotNull(result);

        var actualCoreIds = result.Core.Select(c => c.Id).ToArray();
        Assert.Equal(ExpectedCoreIds, actualCoreIds);

        var actualRecommendedIds = result.Recommended.Select(r => r.Id).ToArray();
        Assert.Equal(ExpectedRecommendedIds, actualRecommendedIds);

        var actualOptionalIds = result.Optional.Select(o => o.Id).ToArray();
        Assert.Equal(ExpectedOptionalIds, actualOptionalIds);

        var actualLanguageIds = result.Languages.Select(l => l.Id).ToArray();
        Assert.Equal(ExpectedLanguageIds, actualLanguageIds);
    }

    [Fact]
    public async Task GetRoleSkills_PublicResponse_DoesNotExposeRubricsOrPrivateEvaluationMetadata()
    {
        var response = await _client.GetAsync("/api/roles/backend-developer/skills");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rawJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rawJson);

        // Thoroughly scan the entire JSON payload to guarantee no private evaluation metadata leaked
        AssertNoForbiddenKeys(doc.RootElement);

        static void AssertNoForbiddenKeys(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    var nameLower = prop.Name.ToLowerInvariant();
                    Assert.False(nameLower.Contains("rubric"), $"Forbidden rubric key found in public API: '{prop.Name}'");
                    Assert.False(nameLower.Contains("expectedsignal"), $"Forbidden expectedSignals key found in public API: '{prop.Name}'");
                    Assert.False(nameLower.Contains("insufficientevidence"), $"Forbidden rubric level found in public API: '{prop.Name}'");

                    AssertNoForbiddenKeys(prop.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    AssertNoForbiddenKeys(item);
                }
            }
        }
    }

    [Fact]
    public async Task GetRoleSkills_UnknownRole_Returns404NotFound()
    {
        var response = await _client.GetAsync("/api/roles/cloud-architect/skills");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Error.Code);
        Assert.Contains("cloud-architect", error.Error.Message);
    }

    [Fact]
    public async Task Seeder_DuplicateSeeding_DoesNotDuplicateRecords()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Explicitly re-seed multiple times to verify idempotency
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        // Check exact record counts
        var rolesCount = await db.Roles.CountAsync();
        Assert.Equal(4, rolesCount); // V3 Multi-role catalog foundation (3 primary demo roles + 1 legacy role)

        var skillsCount = await db.Skills.CountAsync();
        Assert.Equal(22, skillsCount); // 14 competencies + 8 languages

        var roleSkillsCount = await db.RoleSkills.CountAsync();
        Assert.Equal(22, roleSkillsCount);

        var questionsCount = await db.Questions.CountAsync();
        Assert.Equal(48, questionsCount);

        var sourcesCount = await db.Sources.CountAsync();
        Assert.Equal(42, sourcesCount);
    }

    [Fact]
    public async Task Database_QuestionCountAndDifficultyCalibration_AreValid()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var questions = await db.Questions.AsNoTracking().ToListAsync();
        Assert.Equal(48, questions.Count);

        // Group by assessable skills (6 Core + 2 Recommended + 8 Languages = 16)
        var questionsBySkill = questions.GroupBy(q => q.SkillId).ToList();
        Assert.Equal(16, questionsBySkill.Count);

        foreach (var group in questionsBySkill)
        {
            Assert.Equal(3, group.Count());

            var difficulties = group.Select(q => q.Difficulty).ToList();
            Assert.Contains("foundation", difficulties);
            Assert.Contains("applied", difficulties);
            Assert.Contains("advanced-reasoning", difficulties);
            Assert.Equal(3, difficulties.Distinct().Count());
        }

        // Exactly 16 of each difficulty tier
        Assert.Equal(16, questions.Count(q => q.Difficulty == "foundation"));
        Assert.Equal(16, questions.Count(q => q.Difficulty == "applied"));
        Assert.Equal(16, questions.Count(q => q.Difficulty == "advanced-reasoning"));
    }

    [Fact]
    public async Task Database_SourceAndRubricRelationships_AreValid()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var questions = await db.Questions
            .Include(q => q.Rubric)
            .Include(q => q.FrameworkSources)
            .Include(q => q.InterviewEvidence)
            .AsNoTracking()
            .ToListAsync();

        Assert.Equal(48, questions.Count);

        foreach (var q in questions)
        {
            // Each question has a 1-to-1 Rubric
            Assert.NotNull(q.Rubric);
            Assert.False(string.IsNullOrWhiteSpace(q.Rubric.InsufficientEvidence));
            Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Beginner));
            Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Intermediate));
            Assert.False(string.IsNullOrWhiteSpace(q.Rubric.Advanced));

            // Each question has at least 1 Framework source
            Assert.NotEmpty(q.FrameworkSources);

            // If question has interview evidence, status must be interview-practice-supported
            if (q.InterviewEvidence.Count > 0)
            {
                Assert.Equal("interview-practice-supported", q.VerificationStatus);
                foreach (var ie in q.InterviewEvidence)
                {
                    Assert.False(string.IsNullOrWhiteSpace(ie.EvidenceType));
                    Assert.False(string.IsNullOrWhiteSpace(ie.EvidenceStrength));
                    Assert.False(string.IsNullOrWhiteSpace(ie.Notes));
                }
            }
            else
            {
                Assert.Equal("framework-supported-only", q.VerificationStatus);
            }
        }

        // Coverage metrics consistency in database
        var frameworkSupportedOnlyCount = questions.Count(q => q.VerificationStatus == "framework-supported-only");
        var interviewSupportedCount = questions.Count(q => q.VerificationStatus == "interview-practice-supported");

        Assert.Equal(46, frameworkSupportedOnlyCount);
        Assert.Equal(2, interviewSupportedCount);
    }
}
