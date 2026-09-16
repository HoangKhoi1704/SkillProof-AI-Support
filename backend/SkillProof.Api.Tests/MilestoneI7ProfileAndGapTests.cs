using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneI7ProfileAndGapTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IAdaptiveProfileBuilder _profileBuilder;

    public MilestoneI7ProfileAndGapTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _profileBuilder = new AdaptiveProfileBuilder();
    }

    // =========================================================================
    // 1. UNIT TESTS: PURE DETERMINISTIC PROFILE BUILDER
    // =========================================================================

    [Fact]
    public void Test01_CompletedAdaptiveSession_CreatesProfile()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-01",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql", "testing" },
            Status = "completed"
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL & Relational Databases",
            FirstQuestionId = "q-be-sql-02",
            FirstDifficulty = "applied",
            FirstLevel = "Intermediate",
            FirstReason = "Explains indexing and execution plans.",
            FirstEvidence = new List<string> { "Mentioned B-tree indexing", "Described EXPLAIN ANALYZE" },
            Branch = "advanced-reasoning",
            FollowUpQuestionId = "q-be-sql-03",
            FollowUpDifficulty = "advanced-reasoning",
            FollowUpLevel = "Advanced",
            FollowUpReason = "Mastery of isolation levels and concurrency.",
            FollowUpEvidence = new List<string> { "Analyzed repeatable read anomalies", "Described MVCC locks" },
            FinalLevel = "Advanced",
            EvidenceSummary = new List<string> { "Mentioned B-tree indexing", "Described EXPLAIN ANALYZE", "Analyzed repeatable read anomalies", "Described MVCC locks" }
        };

        session.SkillStates["testing"] = new SkillAssessmentState
        {
            SkillId = "testing",
            SkillName = "Testing & Quality Assurance",
            FirstQuestionId = "q-be-test-02",
            FirstDifficulty = "applied",
            FirstLevel = "Beginner",
            FirstReason = "Simple unit test mention.",
            FirstEvidence = new List<string> { "Mentioned Arrange-Act-Assert" },
            Branch = "foundation",
            FollowUpQuestionId = "q-be-test-01",
            FollowUpDifficulty = "foundation",
            FollowUpLevel = "Beginner",
            FollowUpReason = "Understands unit vs integration distinction.",
            FollowUpEvidence = new List<string> { "Identified boundary testing" },
            FinalLevel = "Beginner",
            EvidenceSummary = new List<string> { "Mentioned Arrange-Act-Assert", "Identified boundary testing" }
        };

        var subskills = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["sql"] = new List<string> { "Schema Design & Normalization", "Query Optimization & Indexing", "Transactions & ACID Guarantees", "Execution Plan Analysis" },
            ["testing"] = new List<string> { "Unit & Integration Testing Strategy", "Test Doubles & Mocking", "Boundary & Edge Case Testing", "Regression & TDD Practices" }
        };

        var profile = _profileBuilder.BuildProfile(session, subskills);

        Assert.NotNull(profile);
        Assert.Equal("backend-developer", profile.RoleId);
        Assert.Equal("adaptive", profile.AssessmentType);
        Assert.Equal(2, profile.Skills.Count);
        Assert.NotNull(profile.Summary);
        Assert.Equal(1, profile.Summary.AdvancedCount);
        Assert.Equal(1, profile.Summary.BeginnerCount);
        Assert.Equal(0, profile.Summary.IntermediateCount);
        Assert.Equal(0, profile.Summary.InsufficientEvidenceCount);
        Assert.Equal(2, profile.Summary.TotalSkillsAssessed);
    }

    [Fact]
    public void Test02_OneProfileEntryPerAssessedSkill()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-02",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql", "testing", "rest-api" },
            Status = "completed"
        };

        foreach (var skillId in session.OrderedSkillIds)
        {
            session.SkillStates[skillId] = new SkillAssessmentState
            {
                SkillId = skillId,
                SkillName = skillId.ToUpper(),
                FinalLevel = "Intermediate",
                EvidenceSummary = new List<string> { $"Evidence for {skillId}" }
            };
        }

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        Assert.Equal(3, profile.Skills.Count);
        var skillIds = profile.Skills.Select(s => s.SkillId).ToList();
        Assert.Contains("sql", skillIds);
        Assert.Contains("testing", skillIds);
        Assert.Contains("rest-api", skillIds);
    }

    [Fact]
    public void Test03_FinalLevel_EqualsAggregatedLevel()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-03",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        Assert.Equal("Intermediate", profile.Skills[0].FinalLevel);
    }

    [Fact]
    public void Test04_Evidence_ComesFromEvaluations_AndDeduplicated()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-04",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate",
            EvidenceSummary = new List<string> { "Index scan used", "Index scan used", "Transaction committed" }
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        var strengths = profile.Skills[0].DemonstratedStrengths;
        Assert.Equal(2, strengths.Count);
        Assert.Contains("Index scan used", strengths);
        Assert.Contains("Transaction committed", strengths);
    }

    [Fact]
    public void Test05_NoUnsupportedStrengthClaims_WhenInsufficientEvidence()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-05",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Insufficient Evidence",
            EvidenceSummary = new List<string>()
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        // Must NOT invent strengths
        Assert.Empty(profile.Skills[0].DemonstratedStrengths);
    }

    [Fact]
    public void Test06_InsufficientEvidence_RemainsDistinctFromBeginner()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-06",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql", "testing" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Insufficient Evidence",
            FirstReason = "Answer was blank or off-topic."
        };

        session.SkillStates["testing"] = new SkillAssessmentState
        {
            SkillId = "testing",
            SkillName = "Testing",
            FinalLevel = "Beginner",
            FirstReason = "Basic unit test description."
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        var sqlItem = profile.Skills.First(s => s.SkillId == "sql");
        var testItem = profile.Skills.First(s => s.SkillId == "testing");

        Assert.Equal("Insufficient Evidence", sqlItem.FinalLevel);
        Assert.Equal("Beginner", testItem.FinalLevel);
        Assert.NotEqual(sqlItem.FinalLevel, testItem.FinalLevel);
    }

    [Fact]
    public void Test07_InsufficientEvidence_ProducesEvidenceGapLanguage()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-07",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL & Relational Databases",
            FinalLevel = "Insufficient Evidence",
            FirstReason = "No relevant SQL concepts were provided."
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());
        var gaps = profile.Skills[0].EvidenceGaps;

        Assert.NotEmpty(gaps);
        Assert.Contains(gaps, g => g.Contains("Insufficient Evidence:", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(gaps, g => g.Contains("Failed", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(gaps, g => g.Contains("Weak", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(gaps, g => g.Contains("0%", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test08_DevelopmentGap_DistinguishedFromEvidenceGap()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-08",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql", "testing" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        session.SkillStates["testing"] = new SkillAssessmentState
        {
            SkillId = "testing",
            SkillName = "Testing",
            FinalLevel = "Insufficient Evidence"
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        var sqlGaps = profile.Skills.First(s => s.SkillId == "sql").EvidenceGaps;
        var testGaps = profile.Skills.First(s => s.SkillId == "testing").EvidenceGaps;

        // SQL (Intermediate) produces Development Gap
        Assert.Contains(sqlGaps, g => g.StartsWith("Development Gap:", StringComparison.OrdinalIgnoreCase));
        // Testing (Insufficient Evidence) produces Insufficient Evidence gap
        Assert.Contains(testGaps, g => g.StartsWith("Insufficient Evidence:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test09_NextDevelopmentAreas_UseApprovedCompetencyMetadata()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-09",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        var catalogSubskills = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["sql"] = new List<string> { "Schema Design & Normalization", "Query Optimization & Indexing", "Transactions & ACID Guarantees", "Execution Plan Analysis" }
        };

        var profile = _profileBuilder.BuildProfile(session, catalogSubskills);

        var nextAreas = profile.Skills[0].NextDevelopmentAreas;
        Assert.NotEmpty(nextAreas);
        foreach (var area in nextAreas)
        {
            Assert.Contains(area, catalogSubskills["sql"]);
        }
    }

    [Fact]
    public void Test10_Profile_ContainsNoPercentagesOrArbitraryScores()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-10",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());
        var json = JsonSerializer.Serialize(profile);

        Assert.DoesNotContain("%", json);
        Assert.DoesNotContain("/100", json);
        Assert.DoesNotContain("score", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("probability", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Test11_Profile_ContainsNoRubricOrExpectedSignals()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-11",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());
        var json = JsonSerializer.Serialize(profile);

        Assert.DoesNotContain("expectedSignals", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rubric", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insufficientEvidenceCriteria", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rawResponse", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Test12_RoadmapHandoffContract_Generated()
    {
        var session = new DiagnosticSessionState
        {
            SessionId = "session-test-12",
            RoleId = "backend-developer",
            OrderedSkillIds = new List<string> { "sql" }
        };

        session.SkillStates["sql"] = new SkillAssessmentState
        {
            SkillId = "sql",
            SkillName = "SQL",
            FinalLevel = "Intermediate"
        };

        var profile = _profileBuilder.BuildProfile(session, new Dictionary<string, IReadOnlyList<string>>());

        Assert.NotNull(profile.RoadmapInput);
        Assert.Equal("backend-developer", profile.RoadmapInput.RoleId);
        Assert.Single(profile.RoadmapInput.SkillGaps);
        Assert.Equal("sql", profile.RoadmapInput.SkillGaps[0].SkillId);
        Assert.Equal("Intermediate", profile.RoadmapInput.SkillGaps[0].CurrentLevel);
        Assert.NotNull(profile.RoadmapInput.SkillGaps[0].DevelopmentAreas);
        Assert.NotNull(profile.RoadmapInput.SkillGaps[0].EvidenceGaps);
    }

    // =========================================================================
    // 2. END-TO-END API INTEGRATION TESTS
    // =========================================================================

    [Fact]
    public async Task Test13_EndToEndAdaptiveSession_CompletesWithProfile()
    {
        // 1. Start adaptive session with SQL
        var startRequest = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" }
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startRequest);
        Assert.Equal(HttpStatusCode.OK, startRes.StatusCode);
        var startData = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(startData);
        var sessionId = startData.SessionId;

        // 2. Answer Stage 1 (Applied) -> Beginner -> Branches to foundation
        var ans1 = new AdaptiveAnswerRequest(
            QuestionId: startData.CurrentQuestion!.Id,
            Answer: "I would use a database table and write a simple select query."
        );
        var res1 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", ans1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var data1 = await res1.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(data1);
        Assert.Equal("in-progress", data1.Status);
        Assert.Equal("follow-up", data1.Progress!.Stage);
        Assert.Null(data1.Profile); // Not completed yet

        // 3. Try calling /profile while in-progress -> Should return 400 SESSION_IN_PROGRESS
        var earlyProfileRes = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/profile");
        Assert.Equal(HttpStatusCode.BadRequest, earlyProfileRes.StatusCode);

        // 4. Answer Stage 2 (Foundation) -> Completes session
        var ans2 = new AdaptiveAnswerRequest(
            QuestionId: data1.CurrentQuestion!.Id,
            Answer: "SQL uses relational tables with primary and foreign keys for referential integrity."
        );
        var res2 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", ans2);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var completedData = await res2.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(completedData);
        Assert.Equal("completed", completedData.Status);
        Assert.NotNull(completedData.Profile);

        // Verify profile in completed response
        var profile = completedData.Profile;
        Assert.Equal("backend-developer", profile.RoleId);
        Assert.Equal("adaptive", profile.AssessmentType);
        Assert.Single(profile.Skills);
        Assert.Equal("sql", profile.Skills[0].SkillId);
        Assert.NotEmpty(profile.Skills[0].EvidenceGaps);
        Assert.NotEmpty(profile.Skills[0].NextDevelopmentAreas);
        Assert.NotNull(profile.RoadmapInput);

        // 5. Query public profile endpoint directly: GET /api/diagnostics/adaptive/sessions/{sessionId}/profile
        var profileRes = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/profile");
        Assert.Equal(HttpStatusCode.OK, profileRes.StatusCode);
        var fetchedProfile = await profileRes.Content.ReadFromJsonAsync<CareerReadinessProfile>();
        Assert.NotNull(fetchedProfile);
        Assert.Equal("backend-developer", fetchedProfile.RoleId);
        Assert.Single(fetchedProfile.Skills);

        // Verify privacy: No rubric or signals in profile endpoint
        var rawJson = await profileRes.Content.ReadAsStringAsync();
        Assert.DoesNotContain("expectedSignals", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rubric", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test14_DevAiInspector_InspectAdaptiveSession_DistinguishesAiFromBackendDerivation()
    {
        // 1. Create and complete an adaptive session
        var startRequest = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "rest-api" }
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startRequest);
        var startData = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        var sessionId = startData!.SessionId;

        // Stage 1
        var ans1 = new AdaptiveAnswerRequest(
            QuestionId: startData.CurrentQuestion!.Id,
            Answer: "I use POST for creation, PUT for replacement, and PATCH for partial updates, handling 409 for conflicts and 422 for unprocessable entities with Idempotency-Key header."
        );
        var res1 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", ans1);
        var data1 = await res1.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // Stage 2
        var ans2 = new AdaptiveAnswerRequest(
            QuestionId: data1!.CurrentQuestion!.Id,
            Answer: "Keyset cursor pagination with WHERE id > cursor LIMIT N avoids deep offset scan performance degradation."
        );
        await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", ans2);

        // 2. Query Developer AI Inspector endpoint: GET /api/dev/ai/adaptive/sessions/{sessionId}
        var inspectRes = await _client.GetAsync($"/api/dev/ai/adaptive/sessions/{sessionId}");
        Assert.Equal(HttpStatusCode.OK, inspectRes.StatusCode);

        var inspectData = await inspectRes.Content.ReadFromJsonAsync<DevAdaptiveInspectionDto>();
        Assert.NotNull(inspectData);
        Assert.Equal(sessionId, inspectData.SessionId);
        Assert.Single(inspectData.Skills);

        var skillInspection = inspectData.Skills[0];
        Assert.Equal("rest-api", skillInspection.SkillId);

        // AI Evaluator Output is present
        Assert.NotNull(skillInspection.Stage1Evaluation);
        Assert.Equal("applied", skillInspection.Stage1Evaluation.Stage);
        Assert.Equal("DeterministicDiagnosticEvaluator", skillInspection.Stage1Evaluation.Evaluator);
        Assert.NotEmpty(skillInspection.Stage1Evaluation.ObservedEvidence);

        // Branch decision is recorded
        Assert.NotNull(skillInspection.BranchDecision);
        Assert.Equal("advanced-reasoning", skillInspection.BranchDecision.BranchChosen);

        // Stage 2 evaluation is present
        Assert.NotNull(skillInspection.Stage2Evaluation);

        // Backend derived profile is clearly separated
        Assert.NotNull(skillInspection.BackendDerivedProfile);
        Assert.NotNull(skillInspection.BackendDerivedProfile.FinalAggregatedLevel);
        Assert.Contains("14-rule evidence matrix applied", skillInspection.BackendDerivedProfile.AggregationRule);
        Assert.NotEmpty(skillInspection.BackendDerivedProfile.NextDevelopmentAreas);
    }

    [Fact]
    public async Task Test15_FinancialAnalyst_LegacyFlow_RemainsUnchanged()
    {
        // Verify Financial Analyst questions endpoint remains unchanged
        var faQuestionsRes = await _client.GetAsync("/api/diagnostics/questions?roleId=financial-analyst");
        Assert.Equal(HttpStatusCode.OK, faQuestionsRes.StatusCode);

        // Verify Financial Analyst evaluate endpoint still works directly
        var evalRequest = new EvaluationRequest(
            "financial-analyst",
            new List<DiagnosticAnswerSubmission>
            {
                new(7, "Revenue is recognized when earned, matching expenses to the period in which they generate revenue."),
                new(8, "Working capital measures short-term liquidity: current assets minus current liabilities.")
            }
        );

        var evalRes = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", evalRequest);
        Assert.Equal(HttpStatusCode.OK, evalRes.StatusCode);
        var evalData = await evalRes.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(evalData);
        Assert.Equal("financial-analyst", evalData.RoleId);
        Assert.NotEmpty(evalData.Skills);
    }
}
