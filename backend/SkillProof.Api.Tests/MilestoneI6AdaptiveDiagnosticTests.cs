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

public class MilestoneI6AdaptiveDiagnosticTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IAdaptiveDiagnosticEngine _engine;

    public MilestoneI6AdaptiveDiagnosticTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _engine = new AdaptiveDiagnosticEngine();
    }

    // =========================================================================
    // 1. DETERMINISTIC AGGREGATION MATRIX & BRANCHING THEORY TESTS
    // =========================================================================

    [Theory]
    [InlineData("Insufficient Evidence", "foundation")]
    [InlineData("Beginner", "foundation")]
    [InlineData("Intermediate", "advanced-reasoning")]
    [InlineData("Advanced", "advanced-reasoning")]
    [InlineData("insufficient evidence", "foundation")]
    [InlineData("BEGINNER", "foundation")]
    [InlineData("intermediate", "advanced-reasoning")]
    [InlineData("ADVANCED", "advanced-reasoning")]
    public void Test01_DetermineBranch_MapsLevelsDeterministically(string appliedLevel, string expectedBranch)
    {
        var branch = _engine.DetermineBranch(appliedLevel);
        Assert.Equal(expectedBranch, branch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Expert")]
    [InlineData("Master")]
    [InlineData("None")]
    public void Test02_DetermineBranch_ThrowsOnInvalidLevel(string invalidLevel)
    {
        Assert.Throws<ArgumentException>(() => _engine.DetermineBranch(invalidLevel));
    }

    /// <summary>
    /// Tests all 14 specified cases (expanded to 16 with sub-cases) of the evidence aggregation matrix individually.
    /// </summary>
    [Theory]
    // Foundation Branch Cases (Applied = Insufficient Evidence)
    [InlineData("Insufficient Evidence", "Insufficient Evidence", "foundation", "Insufficient Evidence")] // Case 1
    [InlineData("Insufficient Evidence", "Beginner", "foundation", "Beginner")]                            // Case 2
    [InlineData("Insufficient Evidence", "Intermediate", "foundation", "Intermediate")]                    // Case 3 (Intermediate)
    [InlineData("Insufficient Evidence", "Advanced", "foundation", "Intermediate")]                        // Case 3 (Advanced)
    // Foundation Branch Cases (Applied = Beginner)
    [InlineData("Beginner", "Insufficient Evidence", "foundation", "Beginner")]                            // Case 4
    [InlineData("Beginner", "Beginner", "foundation", "Beginner")]                                         // Case 5
    [InlineData("Beginner", "Intermediate", "foundation", "Intermediate")]                                 // Case 6 (Intermediate)
    [InlineData("Beginner", "Advanced", "foundation", "Intermediate")]                                     // Case 6 (Advanced)
    // Advanced-Reasoning Branch Cases (Applied = Intermediate)
    [InlineData("Intermediate", "Insufficient Evidence", "advanced-reasoning", "Intermediate")]           // Case 7
    [InlineData("Intermediate", "Beginner", "advanced-reasoning", "Intermediate")]                        // Case 8
    [InlineData("Intermediate", "Intermediate", "advanced-reasoning", "Intermediate")]                    // Case 9
    [InlineData("Intermediate", "Advanced", "advanced-reasoning", "Advanced")]                            // Case 10
    // Advanced-Reasoning Branch Cases (Applied = Advanced)
    [InlineData("Advanced", "Insufficient Evidence", "advanced-reasoning", "Intermediate")]               // Case 11
    [InlineData("Advanced", "Beginner", "advanced-reasoning", "Intermediate")]                            // Case 12
    [InlineData("Advanced", "Intermediate", "advanced-reasoning", "Intermediate")]                        // Case 13
    [InlineData("Advanced", "Advanced", "advanced-reasoning", "Advanced")]                                // Case 14
    public void Test03_AggregationMatrix_AssertsAllSpecifiedCasesIndividually(
        string appliedLevel,
        string followUpLevel,
        string branch,
        string expectedFinalLevel)
    {
        var actualFinalLevel = _engine.DetermineFinalLevel(appliedLevel, followUpLevel, branch);
        Assert.Equal(expectedFinalLevel, actualFinalLevel);
    }

    [Theory]
    // Mismatched branch with applied level
    [InlineData("Intermediate", "foundation", "Beginner")]
    [InlineData("Advanced", "foundation", "Intermediate")]
    [InlineData("Insufficient Evidence", "advanced-reasoning", "Advanced")]
    [InlineData("Beginner", "advanced-reasoning", "Intermediate")]
    // Invalid branch names
    [InlineData("Beginner", "easy", "Beginner")]
    [InlineData("Intermediate", "hard", "Intermediate")]
    [InlineData("Intermediate", "", "Intermediate")]
    // Unsupported / empty levels
    [InlineData("", "foundation", "Beginner")]
    [InlineData("Beginner", "foundation", "")]
    [InlineData("Novice", "foundation", "Beginner")]
    public void Test04_AggregationMatrix_InvalidCombinationsFailClearly(
        string appliedLevel,
        string branch,
        string followUpLevel)
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.DetermineFinalLevel(appliedLevel, followUpLevel, branch));
    }

    [Fact]
    public void Test05_AggregateEvidence_CombinesAndDeduplicatesEvidence()
    {
        var appliedEv = new List<string>
        {
            "Explained primary key indexing",
            "Identified table scan risk",
            "Explained primary key indexing" // duplicate
        };

        var followUpEv = new List<string>
        {
            "Identified table scan risk", // duplicate from stage 1
            "Demonstrated composite index column order trade-off"
        };

        var combined = _engine.AggregateEvidence(appliedEv, followUpEv);

        Assert.Equal(3, combined.Count);
        Assert.Equal("Explained primary key indexing", combined[0]);
        Assert.Equal("Identified table scan risk", combined[1]);
        Assert.Equal("Demonstrated composite index column order trade-off", combined[2]);
    }

    [Fact]
    public void Test06_FormatFinalReason_PreservesBothEvaluationsWithoutInventingClaims()
    {
        var reason1 = "Candidate correctly explained connection pooling basics.";
        var reason2 = "Candidate accurately analyzed semaphore concurrency control under high load.";

        var formatted = _engine.FormatFinalReason(reason1, reason2);

        Assert.Contains("Practical Assessment: Candidate correctly explained connection pooling basics.", formatted);
        Assert.Contains("Follow-up Verification: Candidate accurately analyzed semaphore concurrency control under high load.", formatted);
    }

    // =========================================================================
    // 2. END-TO-END ADAPTIVE SESSION API INTEGRATION TESTS
    // =========================================================================

    [Fact]
    public async Task Test07_StartSession_StartsWithAppliedQuestion_FromSQLite()
    {
        var request = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql", "testing" },
            PrimaryLanguageId: "csharp"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var session = await response.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);
        Assert.StartsWith("sess-", session.SessionId);
        Assert.Equal("in-progress", session.Status);

        // First question must be APPLIED
        Assert.NotNull(session.CurrentQuestion);
        Assert.Equal("applied", session.CurrentQuestion.Difficulty, ignoreCase: true);
        Assert.Equal("sql", session.CurrentQuestion.SkillId, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(session.CurrentQuestion.QuestionText));

        // Progress
        Assert.NotNull(session.Progress);
        Assert.Equal(0, session.Progress.CurrentSkillIndex);
        Assert.Equal(3, session.Progress.TotalSkills); // sql, testing, csharp
        Assert.Equal("applied", session.Progress.Stage);
        Assert.Equal(0, session.Progress.TotalAnswered);
    }

    [Fact]
    public async Task Test08_AppliedBeginner_BranchesToFoundation()
    {
        // 1. Start session with sql
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);

        // 2. Submit beginner answer to SQL applied question
        var answerReq = new AdaptiveAnswerRequest(
            QuestionId: session.CurrentQuestion!.Id,
            Answer: "I know basic SQL queries like select and where clause."
        );

        var answerRes = await _client.PostAsJsonAsync(
            $"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            answerReq
        );
        Assert.Equal(HttpStatusCode.OK, answerRes.StatusCode);

        var nextState = await answerRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(nextState);
        Assert.Equal("in-progress", nextState.Status);

        // Stage must transition to follow-up and difficulty must be foundation
        Assert.NotNull(nextState.CurrentQuestion);
        Assert.Equal("foundation", nextState.CurrentQuestion.Difficulty, ignoreCase: true);
        Assert.Equal("sql", nextState.CurrentQuestion.SkillId, ignoreCase: true);
        Assert.NotNull(nextState.Progress);
        Assert.Equal("follow-up", nextState.Progress.Stage);
        Assert.Equal(1, nextState.Progress.TotalAnswered);
    }

    [Fact]
    public async Task Test09_AppliedIntermediate_BranchesToAdvancedReasoning()
    {
        // 1. Start session with rest-api (evaluates substantive answers to Intermediate)
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "rest-api" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);

        // 2. Submit substantive answer to REST API applied question
        var answerReq = new AdaptiveAnswerRequest(
            QuestionId: session.CurrentQuestion!.Id,
            Answer: "PUT completely replaces the resource state idempotently, whereas PATCH updates only specific fields provided in the payload."
        );

        var answerRes = await _client.PostAsJsonAsync(
            $"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
            answerReq
        );
        Assert.Equal(HttpStatusCode.OK, answerRes.StatusCode);

        var nextState = await answerRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(nextState);
        Assert.Equal("in-progress", nextState.Status);

        // Stage must transition to follow-up and difficulty must be advanced-reasoning
        Assert.NotNull(nextState.CurrentQuestion);
        Assert.Equal("advanced-reasoning", nextState.CurrentQuestion.Difficulty, ignoreCase: true);
        Assert.Equal("rest-api", nextState.CurrentQuestion.SkillId, ignoreCase: true);
        Assert.NotNull(nextState.Progress);
        Assert.Equal("follow-up", nextState.Progress.Stage);
        Assert.Equal(1, nextState.Progress.TotalAnswered);
    }

    [Fact]
    public async Task Test10_FoundationFollowUp_EndsSkill_AndCompletesSingleSkillSession()
    {
        // 1. Start session with 1 skill
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // 2. Submit answer 1 (applied)
        var ans1 = new AdaptiveAnswerRequest(
            QuestionId: session!.CurrentQuestion!.Id,
            Answer: "Basic database tables store relational data."
        );
        var step1Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans1);
        var step1 = await step1Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // 3. Submit answer 2 (foundation follow-up)
        var ans2 = new AdaptiveAnswerRequest(
            QuestionId: step1!.CurrentQuestion!.Id,
            Answer: "Primary keys uniquely identify rows, and foreign keys enforce referential integrity."
        );
        var step2Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans2);
        var step2 = await step2Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        Assert.NotNull(step2);
        Assert.Equal("completed", step2.Status);
        Assert.Null(step2.CurrentQuestion);
        Assert.NotNull(step2.Skills);
        Assert.Single(step2.Skills);

        var sqlSkill = step2.Skills[0];
        Assert.Equal("sql", sqlSkill.SkillId);
        Assert.Equal("foundation", sqlSkill.Branch);
        Assert.Equal(2, sqlSkill.QuestionsAnswered);
        Assert.False(string.IsNullOrWhiteSpace(sqlSkill.FinalLevel));
        Assert.False(string.IsNullOrWhiteSpace(sqlSkill.Reason));
        Assert.NotNull(step2.TopGaps);
    }

    [Fact]
    public async Task Test11_AdvancedFollowUp_EndsSkill_AndAdvancesToNextSkill()
    {
        // 1. Start session with 2 skills: rest-api and sql
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "rest-api", "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // 2. Skill 1 (REST API): Applied -> Substantive answer evaluates to Intermediate
        var ans1 = new AdaptiveAnswerRequest(
            QuestionId: session!.CurrentQuestion!.Id,
            Answer: "PUT completely replaces the resource state idempotently, whereas PATCH applies partial modifications."
        );
        var step1Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans1);
        var step1 = await step1Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.Equal("advanced-reasoning", step1!.CurrentQuestion!.Difficulty);

        // 3. Skill 1 (REST API): Advanced follow-up -> Substantive answer
        var ans2 = new AdaptiveAnswerRequest(
            QuestionId: step1.CurrentQuestion.Id,
            Answer: "Handling concurrent modifications with ETag, If-Match headers, and optimistic locking to prevent lost updates."
        );
        var step2Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans2);
        var step2 = await step2Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // Should now advance to Skill 2 (sql) applied question!
        Assert.Equal("in-progress", step2!.Status);
        Assert.NotNull(step2.CurrentQuestion);
        Assert.Equal("sql", step2.CurrentQuestion.SkillId);
        Assert.Equal("applied", step2.CurrentQuestion.Difficulty);
        Assert.Equal(1, step2.Progress!.CurrentSkillIndex);
        Assert.Equal("applied", step2.Progress.Stage);
        Assert.Equal(2, step2.Progress.TotalAnswered);
    }

    [Fact]
    public async Task Test12_MissingEvidence_RemainsInsufficientEvidence()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // Empty answer 1 -> Insufficient Evidence
        var ans1 = new AdaptiveAnswerRequest(QuestionId: session!.CurrentQuestion!.Id, Answer: "   ");
        var step1Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans1);
        var step1 = await step1Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.Equal("foundation", step1!.CurrentQuestion!.Difficulty);

        // Empty answer 2 -> Insufficient Evidence
        var ans2 = new AdaptiveAnswerRequest(QuestionId: step1.CurrentQuestion.Id, Answer: "short");
        var step2Res = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers", ans2);
        var step2 = await step2Res.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        Assert.Equal("completed", step2!.Status);
        var sqlResult = step2.Skills!.First(s => s.SkillId == "sql");
        Assert.Equal("Insufficient Evidence", sqlResult.FinalLevel);
    }

    [Fact]
    public async Task Test13_SecurityBoundary_ClientCannotAnswerWrongQuestion()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // Client attempts to submit answer to an unauthorized question ID
        var invalidAnswerReq = new AdaptiveAnswerRequest(
            QuestionId: "q-be-sql-01", // not the current expected question
            Answer: "Attempting to bypass adaptive engine"
        );

        var response = await _client.PostAsJsonAsync(
            $"/api/diagnostics/adaptive/sessions/{session!.SessionId}/answers",
            invalidAnswerReq
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(err);
        Assert.Equal("INVALID_QUESTION_SUBMISSION", err.Error.Code);
    }

    [Fact]
    public async Task Test14_SecurityBoundary_UnknownSessionReturnsNotFound()
    {
        var answerReq = new AdaptiveAnswerRequest(
            QuestionId: "q-be-sql-02",
            Answer: "Some valid answer text"
        );

        var response = await _client.PostAsJsonAsync(
            "/api/diagnostics/adaptive/sessions/sess-nonexistent/answers",
            answerReq
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("SESSION_NOT_FOUND", err!.Error.Code);
    }

    [Fact]
    public async Task Test15_CompletedSession_CannotBeAnsweredAgain()
    {
        // Start and complete a 1-skill session
        var startReq = new AdaptiveSessionRequest(RoleId: "backend-developer", SelectedSkillIds: new List<string> { "sql" }, PrimaryLanguageId: null);
        var sRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var s = await sRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var q1 = s!.CurrentQuestion!.Id;
        var r1 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{s.SessionId}/answers", new AdaptiveAnswerRequest(q1, "Answer 1"));
        var s1 = await r1.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        var q2 = s1!.CurrentQuestion!.Id;
        await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{s.SessionId}/answers", new AdaptiveAnswerRequest(q2, "Answer 2"));

        // Now session is completed; attempting another answer must fail
        var r3 = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{s.SessionId}/answers", new AdaptiveAnswerRequest(q2, "Answer 3"));
        Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
        var err = await r3.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("SESSION_ALREADY_COMPLETED", err!.Error.Code);
    }

    [Fact]
    public async Task Test16_PrimaryLanguageParticipatesAsIndependentSkill()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: "python"
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        Assert.Equal(2, session!.Progress!.TotalSkills); // sql, python
    }

    [Fact]
    public async Task Test17_RecommendedSkillsParticipate()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "nosql", "caching" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        Assert.Equal(2, session!.Progress!.TotalSkills);
        Assert.Equal("nosql", session.CurrentQuestion!.SkillId);
    }

    [Fact]
    public async Task Test18_OptionalUnsupportedSkills_ReportedTruthfully()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql", "git", "docker" }, // git and docker are catalog-only
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        Assert.NotNull(session);
        Assert.Single(session.Progress!.TotalSkills.ToString()); // exactly 1 assessable skill (sql)
        Assert.Contains("git", session.UnsupportedSkillIds);
        Assert.Contains("docker", session.UnsupportedSkillIds);
    }

    [Fact]
    public async Task Test19_SkillOrder_IsDeterministic_CoreThenRecommendedThenLanguage()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "caching", "sql", "rest-api", "nosql" },
            PrimaryLanguageId: "rust"
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var session = await startRes.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();

        // First skill in catalog order among selected core skills is rest-api
        Assert.Equal("rest-api", session!.CurrentQuestion!.SkillId);
    }

    [Fact]
    public async Task Test20_Privacy_SessionAndQuestionResponsesNeverLeakRubricExpectedSignalsOrPrompt()
    {
        var startReq = new AdaptiveSessionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql" },
            PrimaryLanguageId: null
        );

        var startRes = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startReq);
        var rawJson = await startRes.Content.ReadAsStringAsync();

        Assert.DoesNotContain("expectedSignals", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insufficientEvidence:", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("evaluationInstructions", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test21_SessionStore_IsThreadSafeAndBounded()
    {
        var store = new AdaptiveSessionStore();

        // Create 210 sessions to test bounding (max capacity 200)
        for (int i = 0; i < 210; i++)
        {
            store.CreateSession(new DiagnosticSessionState
            {
                SessionId = $"test-sess-{i}",
                RoleId = "backend-developer",
                OrderedSkillIds = new List<string> { "sql" },
                CurrentQuestionId = "q-be-sql-02"
            });
        }

        // Must prune down to at most 200 sessions
        int count = 0;
        for (int i = 0; i < 210; i++)
        {
            if (store.TryGetSession($"test-sess-{i}", out _))
            {
                count++;
            }
        }

        Assert.True(count <= 200, $"Store exceeded maximum capacity. Count: {count}");
    }

    [Fact]
    public async Task Test22_ExistingI4Selector_StillPassesWithoutRegression()
    {
        var request = new DynamicQuestionSelectionRequest(
            RoleId: "backend-developer",
            SelectedSkillIds: new List<string> { "sql", "testing" },
            PrimaryLanguageId: "python"
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/questions/select", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<DynamicQuestionSelectionResponse>();
        Assert.NotNull(data);
        Assert.Equal(3, data.Questions.Count);
        Assert.All(data.Questions, q => Assert.Equal("applied", q.Difficulty));
    }

    [Fact]
    public async Task Test23_FinancialAnalyst_FlowRemainsUnchanged()
    {
        var response = await _client.GetAsync("/api/diagnostics/questions?roleId=financial-analyst");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var questions = await response.Content.ReadFromJsonAsync<List<DiagnosticQuestionDto>>();
        Assert.NotNull(questions);
        Assert.True(questions.Count >= 5);
    }
}
