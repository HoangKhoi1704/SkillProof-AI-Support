using Microsoft.EntityFrameworkCore;
using SkillProof.Api.Data;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class QuestionService : IQuestionService
{
    private readonly CatalogDbContext? _db;
    private readonly IDiagnosticEvaluator _evaluator;

    private static readonly HashSet<string> ValidRoleIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "backend-developer",
        "financial-analyst"
    };

    public QuestionService(CatalogDbContext? db = null, IDiagnosticEvaluator? evaluator = null)
    {
        _db = db;
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
        var legacyRoleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var validQuestionIds = new HashSet<string>(legacyRoleQuestions.Select(q => q.Id.ToString()), StringComparer.OrdinalIgnoreCase);

        if (normalizedRoleId == "backend-developer" && _db != null)
        {
            var dbQIds = await _db.Questions.AsNoTracking()
                .Where(q => q.RoleId.ToLower() == normalizedRoleId)
                .Select(q => q.Id)
                .ToListAsync(cancellationToken);

            foreach (var qid in dbQIds)
            {
                validQuestionIds.Add(qid);
            }
        }

        foreach (var answer in request.Answers)
        {
            if (!validQuestionIds.Contains(answer.QuestionId))
            {
                return (false, null, $"Question ID {answer.QuestionId} does not belong to role '{normalizedRoleId}'.");
            }
        }

        var response = await _evaluator.EvaluateAsync(normalizedRoleId, request.Answers, cancellationToken);
        return (true, response, null);
    }
}
