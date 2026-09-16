using SkillProof.Api.Data;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class QuestionService : IQuestionService
{
    private readonly IDiagnosticEvaluator _evaluator;

    private static readonly HashSet<string> ValidRoleIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "backend-developer",
        "financial-analyst"
    };

    public QuestionService(IDiagnosticEvaluator? evaluator = null)
    {
        _evaluator = evaluator ?? new DeterministicDiagnosticEvaluator();
    }

    public IReadOnlyList<RoleDto> GetRoles() => SeedData.Roles;

    public bool TryGetQuestions(string roleId, out IReadOnlyList<DiagnosticQuestionDto>? questions)
    {
        if (string.IsNullOrWhiteSpace(roleId) || !ValidRoleIds.Contains(roleId.Trim()))
        {
            questions = null;
            return false;
        }

        var normalizedRoleId = roleId.Trim().ToLowerInvariant();
        questions = SeedData.Questions
            .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
            .Select(q => q.ToPublicDto())
            .ToList();

        return true;
    }

    public InternalQuestion? GetInternalQuestion(int questionId)
    {
        return SeedData.Questions.FirstOrDefault(q => q.Id == questionId);
    }

    public (bool Success, EvaluationResponse? Response, string? ErrorMessage) EvaluateDiagnostic(EvaluationRequest request)
    {
        return EvaluateDiagnosticAsync(request).GetAwaiter().GetResult();
    }

    public async Task<(bool Success, EvaluationResponse? Response, string? ErrorMessage)> EvaluateDiagnosticAsync(
        EvaluationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return (false, null, "Request body cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.RoleId))
        {
            return (false, null, "roleId is required.");
        }

        var normalizedRoleId = request.RoleId.Trim().ToLowerInvariant();
        if (!ValidRoleIds.Contains(normalizedRoleId))
        {
            return (false, null, "roleId must be one of: backend-developer, financial-analyst");
        }

        if (request.Answers == null || request.Answers.Count == 0)
        {
            return (false, null, "At least one diagnostic answer is required.");
        }

        // Validate each questionId belongs to the role
        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var roleQuestionIds = new HashSet<int>(roleQuestions.Select(q => q.Id));

        foreach (var answer in request.Answers)
        {
            if (!roleQuestionIds.Contains(answer.QuestionId))
            {
                return (false, null, $"Question ID {answer.QuestionId} does not belong to role '{normalizedRoleId}'.");
            }
        }

        var response = await _evaluator.EvaluateAsync(normalizedRoleId, request.Answers, cancellationToken);
        return (true, response, null);
    }
}
