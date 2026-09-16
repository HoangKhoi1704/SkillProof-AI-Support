using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using SkillProof.Api.Data;
using SkillProof.Api.Models;
using SkillProof.Api.Services;

namespace SkillProof.Api.Tests;

public class Milestone3EvaluatorTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly DeterministicDiagnosticEvaluator _fallbackEvaluator;
    private readonly OpenAiDiagnosticEvaluator _evaluator;

    private static readonly HashSet<string> ApprovedLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Beginner",
        "Intermediate",
        "Advanced",
        "Insufficient Evidence"
    };

    public Milestone3EvaluatorTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _fallbackEvaluator = new DeterministicDiagnosticEvaluator();
        _evaluator = new OpenAiDiagnosticEvaluator(
            apiKey: "dummy-key-for-unit-tests",
            model: "dummy-model",
            fallbackEvaluator: _fallbackEvaluator,
            logger: NullLogger<OpenAiDiagnosticEvaluator>.Instance
        );
    }

    [Fact]
    public void Validator_Rejects_UnknownQualitativeLevels_AndEnforcesApprovedFour()
    {
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId == "backend-developer")
            .ToList();

        var answers = roleQuestions.ToDictionary(q => q.Id, _ => "Some answer");

        var rawAiOutput = """
        {
          "evaluations": [
            {
              "competency": "REST API",
              "level": "Expert",
              "reason": "Claims expert knowledge.",
              "evidence": ["Evidence 1"]
            },
            {
              "competency": "SQL / Database",
              "level": "Level 4",
              "reason": "Numeric tier attempted.",
              "evidence": ["Evidence 2"]
            },
            {
              "competency": "Testing",
              "level": "Intermediate",
              "reason": "Good testing reasoning.",
              "evidence": ["Uses mocks"]
            }
          ]
        }
        """;

        var response = _evaluator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, roleQuestions, answers);

        Assert.NotNull(response);
        Assert.Equal(6, response.Skills.Count);

        foreach (var skill in response.Skills)
        {
            Assert.Contains(skill.Level, ApprovedLevels);
        }

        // "Expert" and "Level 4" should be converted to "Insufficient Evidence"
        var restSkill = response.Skills.First(s => s.Name == "REST API");
        Assert.Equal("Insufficient Evidence", restSkill.Level);

        var sqlSkill = response.Skills.First(s => s.Name == "SQL / Database");
        Assert.Equal("Insufficient Evidence", sqlSkill.Level);

        var testingSkill = response.Skills.First(s => s.Name == "Testing");
        Assert.Equal("Intermediate", testingSkill.Level);
    }

    [Fact]
    public void Validator_Rejects_MalformedJson_AndReturnsNullForFallback()
    {
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId == "backend-developer")
            .ToList();

        var answers = roleQuestions.ToDictionary(q => q.Id, _ => "Some answer");

        var malformedOutputs = new[]
        {
            "not a json string at all",
            "{ \"evaluations\": \"not an array\" }",
            "{ \"wrong_root\": [] }",
            ""
        };

        foreach (var malformed in malformedOutputs)
        {
            var response = _evaluator.ValidateAndProcessAiResponse("backend-developer", malformed, roleQuestions, answers);
            Assert.Null(response);
        }
    }

    [Fact]
    public void Validator_Rejects_InventedOrInvalidCompetencies()
    {
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId == "backend-developer")
            .ToList();

        var answers = roleQuestions.ToDictionary(q => q.Id, _ => "Some answer");

        var rawAiOutput = """
        {
          "evaluations": [
            {
              "competency": "Quantum Computing",
              "level": "Advanced",
              "reason": "Student claims quantum expertise.",
              "evidence": ["Qubits mentioned"]
            },
            {
              "competency": "REST API",
              "level": "Intermediate",
              "reason": "Distinguishes PUT vs PATCH.",
              "evidence": ["Idempotency"]
            }
          ]
        }
        """;

        var response = _evaluator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, roleQuestions, answers);

        Assert.NotNull(response);
        // Invented competency must not appear in the skill profile
        Assert.DoesNotContain(response.Skills, s => s.Name == "Quantum Computing");

        // Legitimate role competencies must all be accounted for
        Assert.Equal(6, response.Skills.Count);
        Assert.Contains(response.Skills, s => s.Name == "REST API");
        Assert.Contains(response.Skills, s => s.Name == "SQL / Database");
    }

    [Fact]
    public void Validator_Normalizes_EmptyOrMissingReason_AndSanitizesPercentages()
    {
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId == "backend-developer")
            .ToList();

        var answers = roleQuestions.ToDictionary(q => q.Id, _ => "Some answer");

        var rawAiOutput = """
        {
          "evaluations": [
            {
              "competency": "REST API",
              "level": "Intermediate",
              "reason": "   ",
              "evidence": ["Evidence"]
            },
            {
              "competency": "Authentication",
              "level": "Intermediate",
              "reason": "Candidate scored 95% on JWT cryptographic claims verification.",
              "evidence": ["JWT verified"]
            }
          ]
        }
        """;

        var response = _evaluator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, roleQuestions, answers);

        Assert.NotNull(response);
        var restSkill = response.Skills.First(s => s.Name == "REST API");
        Assert.False(string.IsNullOrWhiteSpace(restSkill.Reason));

        var authSkill = response.Skills.First(s => s.Name == "Authentication");
        Assert.DoesNotContain("95%", authSkill.Reason);
        Assert.Contains("rubric-aligned", authSkill.Reason);
    }

    [Fact]
    public void Validator_SafelyHandles_InvalidEvidenceStructure()
    {
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId == "backend-developer")
            .ToList();

        var answers = roleQuestions.ToDictionary(q => q.Id, _ => "Some answer");

        var rawAiOutput = """
        {
          "evaluations": [
            {
              "competency": "REST API",
              "level": "Intermediate",
              "reason": "Valid reason.",
              "evidence": ["", "   ", "Valid quote"]
            }
          ]
        }
        """;

        var response = _evaluator.ValidateAndProcessAiResponse("backend-developer", rawAiOutput, roleQuestions, answers);

        Assert.NotNull(response);
        var restSkill = response.Skills.First(s => s.Name == "REST API");
        Assert.Single(restSkill.Evidence);
        Assert.Equal("Valid quote", restSkill.Evidence[0]);
    }

    [Fact]
    public async Task TopGaps_NeverExceedsThree_AcrossAllEvaluationProfiles()
    {
        var submissions = new List<DiagnosticAnswerSubmission>
        {
            new(1, ""),
            new(2, ""),
            new(3, ""),
            new(4, ""),
            new(5, ""),
            new(6, "")
        };

        var response = await _fallbackEvaluator.EvaluateAsync("backend-developer", submissions);

        Assert.NotNull(response.TopGaps);
        Assert.True(response.TopGaps.Count <= 3);
        Assert.Equal(3, response.TopGaps.Count);
    }

    [Fact]
    public async Task ProviderFailure_InvokesDeterministicFallback_SafelyWithoutException()
    {
        // An evaluator initialized with a non-existent or failing key/endpoint should fall back deterministically
        var failingEvaluator = new OpenAiDiagnosticEvaluator(
            apiKey: "invalid-key-that-will-fail",
            model: "non-existent-model",
            fallbackEvaluator: _fallbackEvaluator,
            logger: NullLogger<OpenAiDiagnosticEvaluator>.Instance
        );

        var submissions = new List<DiagnosticAnswerSubmission>
        {
            new(1, "PUT replaces resource, PATCH modifies partially with idempotency guarantees."),
            new(2, "Inspect EXPLAIN plan and add composite index.")
        };

        // Must not throw; must safely return the deterministic fallback
        var response = await failingEvaluator.EvaluateAsync("backend-developer", submissions);

        Assert.NotNull(response);
        Assert.Equal("backend-developer", response.RoleId);
        Assert.Equal(6, response.Skills.Count);
        Assert.NotEmpty(response.TopGaps);
        Assert.True(response.TopGaps.Count <= 3);
    }

    [Fact]
    public async Task Rubric_IsNeverExposedPublicly_InAnyEndpoint()
    {
        // 1. Check GET /api/diagnostics/questions
        var questionsResponse = await _client.GetAsync("/api/diagnostics/questions?roleId=backend-developer");
        Assert.Equal(HttpStatusCode.OK, questionsResponse.StatusCode);

        var questionsContent = await questionsResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"rubric\"", questionsContent, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RubricDefinition", questionsContent, StringComparison.OrdinalIgnoreCase);

        // 2. Check POST /api/diagnostics/evaluate
        var evalRequest = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new(1, "PUT vs PATCH explanation")
            }
        );

        var evalResponse = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", evalRequest);
        Assert.Equal(HttpStatusCode.OK, evalResponse.StatusCode);

        var evalContent = await evalResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"rubric\"", evalContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Milestone2_EndToEndDiagnosticEvaluation_ContractRemainsIntact()
    {
        var request = new EvaluationRequest(
            "backend-developer",
            new List<DiagnosticAnswerSubmission>
            {
                new(1, "PUT replaces the entire resource representation while PATCH performs a partial update with idempotency guarantees."),
                new(2, "I would inspect the execution plan using EXPLAIN and add a composite index on CustomerId and OrderDate."),
                new(3, "I would use mock interfaces to isolate payment gateway calls and test success and error status codes."),
                new(4, "I would use Redis with a sliding window counter and return HTTP 429 Too Many Requests."),
                new(5, "Stateless JWT verifies cryptographic signature on the header and payload claims using HMAC or RSA."),
                new(6, "I would stream records with IAsyncEnumerable and pass a CancellationToken to allow graceful cancellation.")
            }
        );

        var response = await _client.PostAsJsonAsync("/api/diagnostics/evaluate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        Assert.NotNull(result);
        Assert.Equal("backend-developer", result.RoleId);
        Assert.Equal(6, result.Skills.Count);

        foreach (var skill in result.Skills)
        {
            Assert.Contains(skill.Level, ApprovedLevels);
            Assert.False(string.IsNullOrWhiteSpace(skill.Reason));
        }

        Assert.True(result.TopGaps.Count <= 3);
    }
}
