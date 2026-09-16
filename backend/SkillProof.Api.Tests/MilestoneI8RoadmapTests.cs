using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneI8RoadmapTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DeterministicRoadmapGenerator _deterministicGenerator;
    private readonly OpenAiRoadmapGenerator _openAiGenerator;

    public MilestoneI8RoadmapTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _deterministicGenerator = new DeterministicRoadmapGenerator();
        _openAiGenerator = new OpenAiRoadmapGenerator(
            apiKey: "test-key-mock",
            model: "gpt-5.4-mini",
            fallbackGenerator: _deterministicGenerator,
            logger: NullLogger<OpenAiRoadmapGenerator>.Instance
        );
    }

    [Fact]
    public async Task Test01_GenerateFromHandoff_Uses_Trusted_I7_Handoff()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new() { "Limited advanced reasoning" }),
                new("python", "Python", "Beginner", new() { "Asyncio Concurrency" }, new() { "Missing concurrency evidence" })
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff, "sess-123");

        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal("sess-123", result.SessionId);
        Assert.Equal(2, result.Items.Count);
        Assert.Contains(result.Items, i => i.SkillId == "sql");
        Assert.Contains(result.Items, i => i.SkillId == "python");
    }

    [Fact]
    public async Task Test02_Development_Gap_Generates_Development_Activity()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new() { "Limited advanced reasoning" })
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal("Development Gap", item.GapType);
        Assert.Equal("Intermediate", item.CurrentLevel);
        Assert.Contains("Intermediate", item.WhyThisMatters);
        Assert.Contains("Transaction", item.WhyThisMatters);
        Assert.False(string.IsNullOrWhiteSpace(item.LearningGoal));
        Assert.False(string.IsNullOrWhiteSpace(item.PracticeTask));
        Assert.False(string.IsNullOrWhiteSpace(item.EvidenceTarget));
        Assert.NotNull(item.CompletionCriteria);
        Assert.True(item.CompletionCriteria.Count >= 2);
    }

    [Fact]
    public async Task Test03_Evidence_Gap_Generates_Evidence_Building_Activity_Not_Beginner()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Insufficient Evidence", new() { "Query-plan interpretation" }, new() { "Not enough evidence provided" })
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        Assert.Single(result.Items);
        var item = result.Items[0];
        Assert.Equal("Evidence Gap", item.GapType);
        Assert.Equal("Insufficient Evidence", item.CurrentLevel);
        Assert.NotEqual("Beginner", item.CurrentLevel);
        // Semantics must be neutral evidence-building, not failure or remedial weakness
        Assert.DoesNotContain("Weak", item.WhyThisMatters, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Failed", item.WhyThisMatters, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0%", item.WhyThisMatters);
        Assert.Contains("did not collect sufficient", item.WhyThisMatters, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(item.PracticeTask));
        Assert.False(string.IsNullOrWhiteSpace(item.EvidenceTarget));
    }

    [Fact]
    public async Task Test04_Roadmap_Priority_Deterministic_Ordering_And_Tie_Preservation()
    {
        // Category 1: Dev gaps (Beginner, Intermediate)
        // Category 2: Evidence gaps (Insufficient Evidence)
        // Category 3: Next-level (Advanced)
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sys-des", "System Design", "Advanced", new() { "Distributed Rate Limiting" }, new()),
                new("sql", "SQL / Database", "Insufficient Evidence", new() { "Diagnostic Reasoning" }, new() { "Missing data" }),
                new("testing", "Testing", "Beginner", new() { "Integration Testing" }, new() { "No integration tests" }),
                new("rest-api", "REST API", "Intermediate", new() { "Idempotency" }, new() { "No ETag usage" })
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        Assert.Equal(4, result.Items.Count);
        // Priority 1 and 2 must be the Development Gaps (testing and rest-api in order of appearance)
        Assert.Equal("testing", result.Items[0].SkillId);
        Assert.Equal(1, result.Items[0].Priority);
        Assert.Equal("Development Gap", result.Items[0].GapType);

        Assert.Equal("rest-api", result.Items[1].SkillId);
        Assert.Equal(2, result.Items[1].Priority);
        Assert.Equal("Development Gap", result.Items[1].GapType);

        // Priority 3 must be Evidence Gap (sql)
        Assert.Equal("sql", result.Items[2].SkillId);
        Assert.Equal(3, result.Items[2].Priority);
        Assert.Equal("Evidence Gap", result.Items[2].GapType);

        // Priority 4 must be Next-Level (sys-des)
        Assert.Equal("sys-des", result.Items[3].SkillId);
        Assert.Equal(4, result.Items[3].Priority);
    }

    [Fact]
    public async Task Test05_Maximum_Five_Items_Enforced()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("s1", "Skill 1", "Beginner", new() { "Topic 1" }, new()),
                new("s2", "Skill 2", "Beginner", new() { "Topic 2" }, new()),
                new("s3", "Skill 3", "Intermediate", new() { "Topic 3" }, new()),
                new("s4", "Skill 4", "Intermediate", new() { "Topic 4" }, new()),
                new("s5", "Skill 5", "Insufficient Evidence", new() { "Topic 5" }, new()),
                new("s6", "Skill 6", "Insufficient Evidence", new() { "Topic 6" }, new()),
                new("s7", "Skill 7", "Advanced", new() { "Topic 7" }, new())
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(1, result.Items[0].Priority);
        Assert.Equal(5, result.Items[4].Priority);
    }

    [Fact]
    public async Task Test06_Fewer_Items_Allowed_When_Fewer_Gaps_Exist_No_Fake_Gaps()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new())
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        // Exactly 1 item; must NOT add fake gaps to reach 3 or 5!
        Assert.Single(result.Items);
        Assert.Equal("sql", result.Items[0].SkillId);
    }

    [Fact]
    public async Task Test07_Every_Item_Has_PracticeTask_EvidenceTarget_CompletionCriteria()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new()),
                new("testing", "Testing", "Insufficient Evidence", new() { "Unit Testing" }, new()),
                new("python", "Python", "Advanced", new() { "GIL Optimization" }, new())
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        foreach (var item in result.Items)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.LearningGoal));
            Assert.False(string.IsNullOrWhiteSpace(item.PracticeTask));
            Assert.False(string.IsNullOrWhiteSpace(item.EvidenceTarget));
            Assert.False(string.IsNullOrWhiteSpace(item.WhyThisMatters));
            Assert.NotNull(item.CompletionCriteria);
            Assert.NotEmpty(item.CompletionCriteria);
            Assert.NotNull(item.LearningActions);
            Assert.NotEmpty(item.LearningActions);
        }
    }

    [Fact]
    public void Test08_Structured_Ai_Output_Validation_Rejects_Unknown_Skills_And_Preserves_Levels()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new()),
                new("testing", "Testing", "Beginner", new() { "Integration Testing" }, new())
            }
        );

        // AI returns an unknown injected skill "quantum-computing" and tries to replace SQL's level with "Master"
        var aiJson = """
        {
          "items": [
            {
              "skillId": "quantum-computing",
              "skill": "Quantum Computing",
              "priority": 1,
              "currentLevel": "Expert",
              "gapType": "Development Gap",
              "targetArea": "Qubits",
              "whyThisMatters": "Injected why",
              "learningGoal": "Learn Qiskit",
              "learningActions": ["Qubits", "Gates"],
              "practiceTask": "Build a quantum circuit",
              "evidenceTarget": "Quantum repo",
              "completionCriteria": ["Pass simulation"]
            },
            {
              "skillId": "sql",
              "skill": "SQL / Database",
              "priority": 2,
              "currentLevel": "Master",
              "gapType": "Development Gap",
              "targetArea": "Concurrency",
              "whyThisMatters": "Valid why",
              "learningGoal": "Master isolation",
              "learningActions": ["ACID", "MVCC"],
              "practiceTask": "Simulate concurrent race conditions",
              "evidenceTarget": "Technical report",
              "completionCriteria": ["Prevents lost updates"]
            }
          ]
        }
        """;

        var response = _openAiGenerator.ValidateAndProcessHandoffAiResponse("backend-developer", aiJson, handoff, "sess-test");

        Assert.NotNull(response);
        // The unknown skill must be discarded!
        Assert.Single(response.Items);
        var sqlItem = response.Items[0];
        Assert.Equal("sql", sqlItem.SkillId);
        // The attempted level alteration to "Master" must be rejected and reset to profile's "Intermediate"
        Assert.Equal("Intermediate", sqlItem.CurrentLevel);
        Assert.Equal(1, sqlItem.Priority);
    }

    [Fact]
    public void Test09_Invalid_Structured_Ai_Output_Returns_Null_For_Fallback()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new())
            }
        );

        // Missing required field "evidenceTarget"
        var invalidJson = """
        {
          "items": [
            {
              "skillId": "sql",
              "skill": "SQL / Database",
              "priority": 1,
              "currentLevel": "Intermediate",
              "gapType": "Development Gap",
              "targetArea": "Concurrency",
              "whyThisMatters": "Valid why",
              "learningGoal": "Master isolation",
              "practiceTask": "Simulate concurrent race conditions"
            }
          ]
        }
        """;

        var response = _openAiGenerator.ValidateAndProcessHandoffAiResponse("backend-developer", invalidJson, handoff);
        Assert.Null(response);
    }

    [Fact]
    public async Task Test10_Privacy_Guards_No_Readiness_Score_Rubric_Or_ExpectedSignals()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new())
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);
        var json = JsonSerializer.Serialize(result);

        Assert.DoesNotContain("expectedSignals", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("insufficientEvidenceCriteria", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemInstructions", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rubric", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("score", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/100", json);
        Assert.DoesNotContain("%", json);
    }

    [Fact]
    public async Task Test11_ProjectGapContext_Handoff_For_I9_Generated()
    {
        var handoff = new RoadmapHandoffContract(
            RoleId: "backend-developer",
            SkillGaps: new List<RoadmapSkillGapInput>
            {
                new("sql", "SQL / Database", "Intermediate", new() { "Transaction Isolation" }, new()),
                new("testing", "Testing", "Beginner", new() { "Integration Testing" }, new())
            }
        );

        var result = await _deterministicGenerator.GenerateFromHandoffAsync(handoff);

        Assert.NotNull(result.ProjectContext);
        Assert.Equal("backend-developer", result.ProjectContext.RoleId);
        Assert.Equal(2, result.ProjectContext.TargetSkills.Count);
        Assert.Equal("sql", result.ProjectContext.TargetSkills[0].SkillId);
        Assert.Equal("testing", result.ProjectContext.TargetSkills[1].SkillId);
        Assert.False(string.IsNullOrWhiteSpace(result.ProjectContext.TargetSkills[0].PracticeTask));
        Assert.False(string.IsNullOrWhiteSpace(result.ProjectContext.TargetSkills[0].EvidenceTarget));
    }

    [Fact]
    public async Task Test12_Api_PostAdaptiveRoadmap_CompletedSession_Returns_Roadmap()
    {
        // 1. Initialize adaptive session with SQL
        var startResp = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", new
        {
            roleId = "backend-developer",
            selectedSkillIds = new[] { "sql" }
        });
        var startData = await startResp.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(startData);
        var sessionId = startData.SessionId;

        // Stage 1 answer (produces Intermediate -> follow-up advanced-reasoning)
        await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", new
        {
            questionId = startData.CurrentQuestion!.Id,
            answer = "I analyze query execution plans with EXPLAIN and apply indexes on selective query filters."
        });

        // Fetch stage 2
        var s2Resp = await _client.GetFromJsonAsync<AdaptiveSessionResponse>($"/api/diagnostics/adaptive/sessions/{sessionId}");
        Assert.NotNull(s2Resp?.CurrentQuestion);

        // Stage 2 answer
        var completeResp = await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", new
        {
            questionId = s2Resp.CurrentQuestion.Id,
            answer = "I consider transaction boundaries, isolation levels, and locking trade-offs under concurrent updates."
        });
        Assert.Equal(HttpStatusCode.OK, completeResp.StatusCode);

        // 2. Call POST /api/diagnostics/adaptive/sessions/{sessionId}/roadmap
        var roadmapResp = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/roadmap", null);
        Assert.Equal(HttpStatusCode.OK, roadmapResp.StatusCode);

        var roadmap = await roadmapResp.Content.ReadFromJsonAsync<RoadmapResponse>();
        Assert.NotNull(roadmap);
        Assert.Equal("backend-developer", roadmap.RoleId);
        Assert.Single(roadmap.Items);
        Assert.Equal("sql", roadmap.Items[0].SkillId);
        Assert.Equal("Development Gap", roadmap.Items[0].GapType);
        Assert.False(string.IsNullOrWhiteSpace(roadmap.Items[0].WhyThisMatters));
        Assert.False(string.IsNullOrWhiteSpace(roadmap.Items[0].PracticeTask));
        Assert.False(string.IsNullOrWhiteSpace(roadmap.Items[0].EvidenceTarget));
    }

    [Fact]
    public async Task Test13_Client_Cannot_Inject_Arbitrary_Skill_Gaps_Or_Replace_Levels()
    {
        // 1. Initialize adaptive session with testing
        var startResp = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", new
        {
            roleId = "backend-developer",
            selectedSkillIds = new[] { "testing" }
        });
        var startData = await startResp.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(startData);
        var sessionId = startData.SessionId;

        // Answer stage 1 with blank -> Insufficient Evidence
        await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", new
        {
            questionId = startData.CurrentQuestion!.Id,
            answer = ""
        });

        // Answer stage 2 with blank -> Completes session with Insufficient Evidence
        var s2Resp = await _client.GetFromJsonAsync<AdaptiveSessionResponse>($"/api/diagnostics/adaptive/sessions/{sessionId}");
        await _client.PostAsJsonAsync($"/api/diagnostics/adaptive/sessions/{sessionId}/answers", new
        {
            questionId = s2Resp!.CurrentQuestion!.Id,
            answer = ""
        });

        // 2. Malicious client tries calling /api/roadmaps/generate with sessionId but passing fake skills and levels
        var fakeRequest = new GenerateRoadmapRequest(
            RoleId: "backend-developer",
            TopGaps: new List<string> { "Fake Hacked Skill", "Quantum Security" },
            Skills: new List<RoadmapSkillInput>
            {
                new("testing", "Expert") // Attempt to overwrite level to Expert
            },
            SessionId: sessionId
        );

        var generateResp = await _client.PostAsJsonAsync("/api/roadmaps/generate", fakeRequest);
        Assert.Equal(HttpStatusCode.OK, generateResp.StatusCode);

        var roadmap = await generateResp.Content.ReadFromJsonAsync<RoadmapResponse>();
        Assert.NotNull(roadmap);
        // The server-side handoff MUST be used strictly!
        Assert.Single(roadmap.Items);
        Assert.Equal("testing", roadmap.Items[0].SkillId);
        // Must remain Insufficient Evidence, NOT the client's injected "Expert"!
        Assert.Equal("Insufficient Evidence", roadmap.Items[0].CurrentLevel);
        Assert.DoesNotContain(roadmap.Items, i => i.Skill.Contains("Hacked") || i.Skill.Contains("Quantum"));
    }

    [Fact]
    public async Task Test14_Financial_Analyst_Legacy_Flow_Unchanged()
    {
        var legacyRequest = new GenerateRoadmapRequest(
            RoleId: "financial-analyst",
            TopGaps: new List<string> { "Financial Statements", "Excel / Spreadsheets" },
            Skills: new List<RoadmapSkillInput>
            {
                new("Financial Statements", "Beginner"),
                new("Excel / Spreadsheets", "Intermediate")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/roadmaps/generate", legacyRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoadmapResponse>();
        Assert.NotNull(result);
        Assert.Equal("financial-analyst", result.RoleId);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Financial Statements", result.Items[0].Skill);
        Assert.Equal("Excel / Spreadsheets", result.Items[1].Skill);
    }
}
