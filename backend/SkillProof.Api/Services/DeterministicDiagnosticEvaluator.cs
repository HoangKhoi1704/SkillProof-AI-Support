using SkillProof.Api.Data;
using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicDiagnosticEvaluator : IDiagnosticEvaluator
{
    private static readonly HashSet<string> ApprovedLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Beginner",
        "Intermediate",
        "Advanced",
        "Insufficient Evidence"
    };

    public Task<EvaluationResponse> EvaluateAsync(
        string roleId,
        IReadOnlyList<DiagnosticAnswerSubmission> submissions,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleId = roleId.Trim().ToLowerInvariant();

        var roleQuestions = SeedData.Questions
            .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var answersByQuestionId = submissions
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Answer);

        var skillEvaluations = new List<SkillEvaluationItem>();

        foreach (var q in roleQuestions)
        {
            answersByQuestionId.TryGetValue(q.Id, out var rawAnswer);
            var trimmedAnswer = rawAnswer?.Trim() ?? string.Empty;

            // Empty or unusable answers produce Insufficient Evidence
            if (string.IsNullOrWhiteSpace(trimmedAnswer) || trimmedAnswer.Length < 15)
            {
                skillEvaluations.Add(new SkillEvaluationItem(
                    q.Competency,
                    "Insufficient Evidence",
                    $"The answer submitted for {q.Competency} is empty or lacks sufficient technical explanation to assess competence.",
                    new List<string>()
                ));
                continue;
            }

            var (level, reason, evidence) = EvaluateSubstantiveAnswer(normalizedRoleId, q.Id, q.Competency, trimmedAnswer);

            // Safety guardrail: enforce approved levels strictly
            var safeLevel = ApprovedLevels.Contains(level) ? level : "Insufficient Evidence";
            skillEvaluations.Add(new SkillEvaluationItem(q.Competency, safeLevel, reason, evidence));
        }

        var topGaps = CalculateTopGaps(skillEvaluations);

        return Task.FromResult(new EvaluationResponse(normalizedRoleId, skillEvaluations, topGaps));
    }

    public static List<string> CalculateTopGaps(IReadOnlyList<SkillEvaluationItem> skills)
    {
        // Prioritize at most 3 top gaps: priority given to Insufficient Evidence and Beginner, then Intermediate
        var prioritizedGaps = skills
            .Where(s => s.Level is "Insufficient Evidence" or "Beginner")
            .Select(s => s.Name)
            .Take(3)
            .ToList();

        if (prioritizedGaps.Count < 3)
        {
            var remainingGaps = skills
                .Where(s => s.Level == "Intermediate" && !prioritizedGaps.Contains(s.Name))
                .Select(s => s.Name)
                .Take(3 - prioritizedGaps.Count);

            prioritizedGaps.AddRange(remainingGaps);
        }

        if (prioritizedGaps.Count == 0 && skills.Count > 0)
        {
            prioritizedGaps = skills.Take(3).Select(s => s.Name).ToList();
        }

        return prioritizedGaps.Take(3).ToList();
    }

    private static (string Level, string Reason, List<string> Evidence) EvaluateSubstantiveAnswer(
        string roleId,
        int questionId,
        string competency,
        string answer)
    {
        if (roleId == "backend-developer")
        {
            switch (questionId)
            {
                case 1: // REST API
                    return (
                        "Intermediate",
                        "Accurately distinguishes PUT vs PATCH semantics and idempotency considerations.",
                        new List<string> { "Explains complete resource replacement vs partial update", "Mentions idempotency guarantees" }
                    );

                case 2: // SQL / Database -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Understands basic queries but does not yet explain indexing, composite keys, or query execution plan optimization clearly.",
                        new List<string> { "Identified simple query checks but omitted composite indexing concepts" }
                    );

                case 3: // Testing -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Understands unit testing concepts but does not yet cover mocking, dependency isolation, or edge cases consistently.",
                        new List<string> { "Recognizes unit testing but lacks mock isolation strategy" }
                    );

                case 4: // System Design -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Proposes basic in-memory caching but lacks distributed rate-limiting, cache failure mitigation, and stateless scaling reasoning.",
                        new List<string> { "Mentioned rate limiting but lacked distributed cache strategy" }
                    );

                case 5: // Authentication
                    return (
                        "Intermediate",
                        "Understands stateless JWT token validation, claims, and signature verification.",
                        new List<string> { "Explained token structure and cryptographic signature verification" }
                    );

                case 6: // Programming Fundamentals
                    return (
                        "Intermediate",
                        "Demonstrates understanding of asynchronous pipelines, memory management, and cancellation token propagation.",
                        new List<string> { "Mentions async/await and CancellationToken" }
                    );
            }
        }
        else if (roleId == "financial-analyst")
        {
            switch (questionId)
            {
                case 7: // Financial Statements
                    return (
                        "Intermediate",
                        "Accurately traces Net Income through the Cash Flow Statement to Retained Earnings on the Balance Sheet.",
                        new List<string> { "Connected net income to operating cash flow and retained earnings" }
                    );

                case 8: // Excel / Spreadsheets
                    return (
                        "Intermediate",
                        "Demonstrates proper use of lookup functions and formula error handling.",
                        new List<string> { "Utilized lookup formulas and error handling" }
                    );

                case 9: // Financial Modeling -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Shows basic financial model awareness but lacks SaaS operational driver linkages and dynamic scenario analysis.",
                        new List<string> { "Understands 3-statement basics but lacks dynamic driver linkages" }
                    );

                case 10: // Forecasting -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Identifies variance between revenue and cash, but does not fully decompose working capital deterioration or cash conversion cycle.",
                        new List<string> { "Identified cash divergence but omitted cash conversion cycle decomposition" }
                    );

                case 11: // Ratio Analysis
                    return (
                        "Intermediate",
                        "Correctly calculates liquidity ratios and interprets the divergence between Current and Quick ratios.",
                        new List<string> { "Calculated current and quick ratios accurately" }
                    );

                case 12: // Data Analysis -> Diagnosed Gap
                    return (
                        "Beginner",
                        "Suggests general sales trend inspection but lacks a quantitative margin decomposition framework.",
                        new List<string> { "Proposed basic sales review but lacked unit-economic waterfall breakdown" }
                    );
            }
        }

        return (
            "Beginner",
            $"Demonstrates basic awareness of {competency}.",
            new List<string> { "Provides basic conceptual answer" }
        );
    }
}
