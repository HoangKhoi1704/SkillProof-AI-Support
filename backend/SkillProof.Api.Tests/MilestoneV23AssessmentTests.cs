using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV23AssessmentTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public MilestoneV23AssessmentTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task V23_FrontendAssessment_MandatoryCompositionAndDeduplication()
    {
        // Client requests a subset of skills
        var request = new AdaptiveSessionRequest(
            RoleId: "frontend-developer",
            SelectedSkillIds: new List<string> { "frontend.html-core", "shared.javascript" },
            PrimaryLanguageId: null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("frontend-developer", session.RoleId);

        // Server-side mandatory fundamentals must be present and deduplicated
        var expectedMandatory = new[] { "shared.internet-http", "frontend.html-core", "shared.javascript", "frontend.web-security" };
        Assert.NotNull(session.CurrentQuestion);
        Assert.Contains(session.CurrentQuestion.SkillId, expectedMandatory);

        // Total skills planned in session progress should cover all 4 mandatory fundamentals
        Assert.NotNull(session.Progress);
        Assert.True(session.Progress.TotalSkills >= 4, $"Expected at least 4 mandatory skills, got {session.Progress.TotalSkills}");
    }

    [Fact]
    public async Task V23_QuestionPrivacy_BeforeAndAfterSubmission()
    {
        var request = new AdaptiveSessionRequest(
            RoleId: "frontend-developer",
            SelectedSkillIds: new List<string> { "frontend.html-core" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", request);
        var rawStartJson = await startStartReadAsync(startRes);

        // BEFORE submission: Public question DTO must NEVER contain rubric, expectedSignals, or referenceExplanation
        Assert.DoesNotContain("expectedSignals", rawStartJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rubric", rawStartJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("referenceExplanation", rawStartJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", rawStartJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evaluationInstructions", rawStartJson, StringComparison.OrdinalIgnoreCase);

        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);
        Assert.NotNull(session.CurrentQuestion);

        // SUBMIT ANSWER
        var answerReq = new AdaptiveAnswerRequest(
            QuestionId: session.CurrentQuestion.Id,
            Answer: "I use semantic HTML5 elements like header, nav, main, and article with proper ARIA attributes to ensure screen reader accessibility and keyboard navigation."
        );

        var submitRes = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", answerReq);
        Assert.Equal(HttpStatusCode.OK, submitRes.StatusCode);

        var rawSubmitJson = await submitRes.Content.ReadAsStringAsync();

        // AFTER submission: Safe explanation fields are returned
        Assert.Contains("lastExplanation", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("whatYouCovered", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("whatCouldBeStronger", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("referenceExplanation", rawSubmitJson, StringComparison.OrdinalIgnoreCase);

        // Still must NOT leak raw rubrics, expectedSignals, or system prompts
        Assert.DoesNotContain("\"expectedSignals\":", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"rubric\":", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", rawSubmitJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evaluationInstructions", rawSubmitJson, StringComparison.OrdinalIgnoreCase);

        var updatedSession = await submitRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(updatedSession);
        Assert.NotNull(updatedSession.LastExplanation);
        Assert.NotEmpty(updatedSession.LastExplanation.ReferenceExplanation);

        // EXPLICIT NEXT QUESTION TRANSITION
        var nextRes = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/next", null);
        Assert.Equal(HttpStatusCode.OK, nextRes.StatusCode);

        var advancedSession = await nextRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(advancedSession);
        // After advancing, lastExplanation is cleared
        Assert.Null(advancedSession.LastExplanation);
    }

    [Fact]
    public async Task V23_DataAnalystAssessment_MandatoryFundamentalsAndNonCoreInvariants()
    {
        var request = new AdaptiveSessionRequest(
            RoleId: "data-analyst",
            SelectedSkillIds: new List<string> { "da.python-data-wrangling" },
            PrimaryLanguageId: null
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);
        Assert.Equal("data-analyst", session.RoleId);

        // Verify Data Analyst mandatory fundamentals:
        // shared.sql-fundamentals, da.descriptive-stats, da.python-data-wrangling, da.data-cleaning, da.eda-techniques, da.business-reporting
        Assert.NotNull(session.Progress);
        Assert.True(session.Progress.TotalSkills >= 6, $"Expected at least 6 mandatory fundamentals, got {session.Progress.TotalSkills}");

        // Deep learning invariant: data-analyst.deep-learning must NOT be in mandatory fundamentals
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var dlNode = await db.RoleRoadmapNodes.FirstOrDefaultAsync(n => n.RoleId == "data-analyst" && n.CanonicalSkillId == "data-analyst.deep-learning");
        Assert.NotNull(dlNode);
        Assert.False(dlNode.MandatoryFundamental, "Deep Learning must remain non-mandatory for Data Analyst.");
        Assert.True(dlNode.IsOptional, "Deep Learning must remain optional for Data Analyst.");
    }

    [Fact]
    public async Task V23_FrontendMetaFramework_RemainsNonMandatoryInvariant()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var nextNode = await db.RoleRoadmapNodes.FirstOrDefaultAsync(n => n.RoleId == "frontend-developer" && n.CanonicalSkillId == "frontend.meta-frameworks");
        Assert.NotNull(nextNode);
        Assert.False(nextNode.MandatoryFundamental, "Meta-frameworks (Next.js/Remix/Astro) must remain non-mandatory for Frontend Developer.");
    }

    [Fact]
    public async Task V23_SkillMatrix_ContainsDistinctNotAssessedAndRoleCoverageGap()
    {
        // Run a short backend session
        var request = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", request);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);

        // Answer questions to complete the skill
        while (session.Status != "completed" && session.CurrentQuestion != null)
        {
            var answerReq = new AdaptiveAnswerRequest(
                QuestionId: session.CurrentQuestion.Id,
                Answer: "I use indexed foreign keys, transactional isolation levels, and EXPLAIN ANALYZE to optimize queries and avoid full table scans."
            );
            var subRes = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", answerReq);
            session = await subRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
            Assert.NotNull(session);

            if (session.Status != "completed")
            {
                var nextRes = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/next", null);
                session = await nextRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
                Assert.NotNull(session);
            }
        }

        // Fetch career profile
        var profileRes = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/profile");
        Assert.Equal(HttpStatusCode.OK, profileRes.StatusCode);

        var profile = await profileRes.Content.ReadFromJsonAsync<CareerReadinessProfile>();
        Assert.NotNull(profile);
        Assert.NotNull(profile.SkillMatrix);
        Assert.NotEmpty(profile.SkillMatrix);

        // Verify Not Assessed is distinct from Beginner and mapped to ROLE COVERAGE GAP
        var notAssessedItems = profile.SkillMatrix.Where(i => i.OverallStatus == "Not Assessed").ToList();
        Assert.NotEmpty(notAssessedItems);
        Assert.All(notAssessedItems, item =>
        {
            Assert.Equal("ROLE COVERAGE GAP", item.GapType);
            Assert.NotEqual("Beginner", item.OverallStatus);
        });

        // Verify zero numeric scores or percentages
        var rawProfileJson = await profileRes.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"score\":", rawProfileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"percentage\":", rawProfileJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"numericScore\":", rawProfileJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task V23_Seeder_V3QuestionsTableAndFrozenBackendPreserved()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Re-seed multiple times to test idempotency
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        // V2.0 Frozen Backend Questions remain strictly 48
        var backendQCount = await db.Questions.CountAsync();
        Assert.Equal(48, backendQCount);

        // V3 Questions table has 34 questions (12 frontend + 22 data analyst)
        var v3QCount = await db.V3Questions.CountAsync();
        Assert.Equal(34, v3QCount);

        // Catalog Skills remain 22
        var skillCount = await db.Skills.CountAsync();
        Assert.Equal(22, skillCount);

        // Canonical Skills remain 59
        var canonicalCount = await db.CanonicalSkills.CountAsync();
        Assert.Equal(59, canonicalCount);
    }

    private static async Task<string> startStartReadAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadAsStringAsync();
    }
}
