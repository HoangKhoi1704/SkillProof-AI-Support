using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SkillProof.Api.Models;
using SkillProof.Api.Services;

namespace SkillProof.Api.Tests;

public class Milestone4RoadmapTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DeterministicRoadmapGenerator _deterministicGenerator;
    private readonly OpenAiRoadmapGenerator _openAiGenerator;

    public Milestone4RoadmapTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _deterministicGenerator = new DeterministicRoadmapGenerator();
        _openAiGenerator = new OpenAiRoadmapGenerator(
            apiKey: "dummy-key-for-unit-tests",
            model: "dummy-model",
            fallbackGenerator: _deterministicGenerator,
            logger: NullLogger<OpenAiRoadmapGenerator>.Instance
        );
    }

    [Fact]
    public async Task DeterministicRoadmap_GeneratesMaxThreePriorities_MappedOnlyToTopGaps()
    {
        var request = new GenerateRoadmapRequest(
            "backend-developer",
            new List<string> { "SQL / Database", "Testing", "System Design", "Extraneous Gap" },
            new List<RoadmapSkillInput>
            {
                new("SQL / Database", "Insufficient Evidence"),
                new("Testing", "Beginner"),
                new("System Design", "Beginner")
            }
        );

        var result = await _deterministicGenerator.GenerateAsync(request);

        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        // Maximum 3 priorities enforced
        Assert.Equal(3, result.Items.Count);

        // Sequence matches topGaps order
        Assert.Equal("SQL / Database", result.Items[0].Skill);
        Assert.Equal(1, result.Items[0].Priority);
        Assert.False(string.IsNullOrWhiteSpace(result.Items[0].LearningGoal));
        Assert.False(string.IsNullOrWhiteSpace(result.Items[0].PracticeTask));

        Assert.Equal("Testing", result.Items[1].Skill);
        Assert.Equal(2, result.Items[1].Priority);

        Assert.Equal("System Design", result.Items[2].Skill);
        Assert.Equal(3, result.Items[2].Priority);
    }

    [Fact]
    public void OpenAiValidator_Rejects_InventedSkills_AndFiltersToTopGaps()
    {
        var expectedGaps = new List<string> { "SQL / Database", "Testing" };

        var rawAiOutput = """
        {
          "items": [
            {
              "skill": "Quantum Computing",
              "priority": 1,
              "learningGoal": "Invented goal.",
              "practiceTask": "Invented task."
            },
            {
              "skill": "SQL / Database",
              "priority": 2,
              "learningGoal": "Master query execution plan analysis and composite indexing.",
              "practiceTask": "Analyze slow query execution plan on 10M rows."
            }
          ]
        }
        """;

        var result = _openAiGenerator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, expectedGaps);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);

        // Invented skill must not be present
        Assert.DoesNotContain(result.Items, i => i.Skill == "Quantum Computing");

        // Expected gaps must be present in sequential priority
        Assert.Equal("SQL / Database", result.Items[0].Skill);
        Assert.Equal(1, result.Items[0].Priority);

        // Second gap had no valid AI item, so deterministic fallback was generated
        Assert.Equal("Testing", result.Items[1].Skill);
        Assert.Equal(2, result.Items[1].Priority);
    }

    [Fact]
    public void OpenAiValidator_Rejects_MalformedJson_AndReturnsNullForFallback()
    {
        var expectedGaps = new List<string> { "SQL / Database" };

        var malformedOutputs = new[]
        {
            "not json at all",
            "{ \"items\": \"string instead of array\" }",
            "{ \"unknown_root\": [] }",
            ""
        };

        foreach (var badJson in malformedOutputs)
        {
            var result = _openAiGenerator.ValidateAndProcessAiResponse("backend-developer", badJson, expectedGaps);
            Assert.Null(result);
        }
    }

    [Fact]
    public void OpenAiValidator_Rejects_EmptyPracticeTaskOrLearningGoal()
    {
        var expectedGaps = new List<string> { "Testing" };

        var rawAiOutput = """
        {
          "items": [
            {
              "skill": "Testing",
              "priority": 1,
              "learningGoal": "Valid goal",
              "practiceTask": "   "
            }
          ]
        }
        """;

        var result = _openAiGenerator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, expectedGaps);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        // Empty practice task was rejected and replaced with safe fallback
        Assert.False(string.IsNullOrWhiteSpace(result.Items[0].PracticeTask));
    }

    [Fact]
    public async Task ProviderFailure_InvokesDeterministicFallback_SafelyWithoutException()
    {
        var failingGenerator = new OpenAiRoadmapGenerator(
            apiKey: "invalid-key",
            model: "invalid-model",
            fallbackGenerator: _deterministicGenerator,
            logger: NullLogger<OpenAiRoadmapGenerator>.Instance
        );

        var request = new GenerateRoadmapRequest(
            "backend-developer",
            new List<string> { "Testing", "System Design" },
            new List<RoadmapSkillInput>()
        );

        // Must not throw exception; must return valid deterministic fallback
        var result = await failingGenerator.GenerateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Testing", result.Items[0].Skill);
        Assert.Equal(1, result.Items[0].Priority);
    }

    [Fact]
    public async Task ApiEndpoint_PostRoadmapGenerate_ValidRequest_Returns200Ok_AndConformsToContract()
    {
        var request = new GenerateRoadmapRequest(
            "backend-developer",
            new List<string> { "SQL / Database", "Testing", "System Design" },
            new List<RoadmapSkillInput>
            {
                new("SQL / Database", "Insufficient Evidence"),
                new("Testing", "Beginner"),
                new("System Design", "Beginner")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/roadmaps/generate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<RoadmapResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal(3, result.Items.Count);

        for (int i = 0; i < result.Items.Count; i++)
        {
            var item = result.Items[i];
            Assert.Equal(i + 1, item.Priority);
            Assert.False(string.IsNullOrWhiteSpace(item.Skill));
            Assert.False(string.IsNullOrWhiteSpace(item.LearningGoal));
            Assert.False(string.IsNullOrWhiteSpace(item.PracticeTask));
        }
    }

    [Fact]
    public async Task ApiEndpoint_PostRoadmapGenerate_InvalidRoleId_Returns400BadRequest()
    {
        var request = new GenerateRoadmapRequest(
            "invalid-role-slug",
            new List<string> { "SQL / Database" },
            new List<RoadmapSkillInput>()
        );

        var response = await _client.PostAsJsonAsync("/api/roadmaps/generate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoint_PostRoadmapGenerate_EmptyTopGaps_Returns400BadRequest()
    {
        var request = new GenerateRoadmapRequest(
            "backend-developer",
            new List<string>(),
            new List<RoadmapSkillInput>()
        );

        var response = await _client.PostAsJsonAsync("/api/roadmaps/generate", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
