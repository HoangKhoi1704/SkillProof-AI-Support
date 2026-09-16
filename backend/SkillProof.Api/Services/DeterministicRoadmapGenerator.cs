using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicRoadmapGenerator : IRoadmapGenerator
{
    private static readonly Dictionary<string, (string LearningGoal, string PracticeTask)> BackendDeveloperGuidance =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SQL / Database"] = (
                "Master query execution plan analysis (EXPLAIN), composite indexing strategies, and multi-table join mechanics.",
                "Analyze a slow query execution plan across a 10M+ row Orders schema and benchmark latency before and after applying a composite index on (CustomerId, OrderDate)."
            ),
            ["Testing"] = (
                "Learn automated unit testing with dependency isolation, test doubles/mocks, and boundary edge cases.",
                "Write an automated test suite for a third-party payment integration mocking transient network timeouts, gateway rejections, and currency rounding."
            ),
            ["System Design"] = (
                "Understand distributed rate limiting, cache failure mitigation strategies, and stateless API scaling.",
                "Design and prototype a Redis sliding-window counter rate limiter returning HTTP 429 and Retry-After headers under concurrent load."
            ),
            ["REST API"] = (
                "Deepen understanding of HTTP specification semantics, idempotent resource mutation (PUT vs PATCH), and concurrency control with ETags.",
                "Implement a partial-resource PATCH endpoint using JSON Merge Patch with optimistic concurrency checks using If-Match."
            ),
            ["Authentication"] = (
                "Master stateless JWT validation, asymmetric cryptographic signing (RSA/ECDSA), and token revocation patterns.",
                "Build a token refresh service with cryptographic signature verification and short-lived access tokens backed by sliding refresh token rotation."
            ),
            ["Programming Fundamentals"] = (
                "Master asynchronous streaming pipelines, bounded parallelism, and non-blocking cancellation token propagation.",
                "Implement an asynchronous background batch pipeline using IAsyncEnumerable and CancellationToken to process 50k records with bounded memory usage."
            )
        };

    private static readonly Dictionary<string, (string LearningGoal, string PracticeTask)> FinancialAnalystGuidance =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Financial Statements"] = (
                "Master the dynamic linkage between the Income Statement, Cash Flow Statement, and Balance Sheet.",
                "Build a dynamic 3-statement financial model tracing a $10M Net Income increase through working capital adjustments to Retained Earnings."
            ),
            ["Excel / Spreadsheets"] = (
                "Master dynamic array formulas, advanced lookup functions (XLOOKUP, INDEX/MATCH), and audit-proof model architecture.",
                "Construct an automated monthly revenue dashboard with structured table references and error handling (IFERROR/IFNA)."
            ),
            ["Financial Modeling"] = (
                "Learn operational SaaS driver modeling, revenue cohort retention, and dynamic scenario toggles.",
                "Build a multi-scenario SaaS financial forecast linking active ARR cohorts to cash burn and runway."
            ),
            ["Forecasting"] = (
                "Deconstruct working capital deterioration and analyze cash conversion cycle divergence.",
                "Analyze a business scenario where revenue growth masks operating cash flow depletion and calculate DSO, DIO, and DPO."
            ),
            ["Ratio Analysis"] = (
                "Interpret divergence between liquidity ratios and analyze capital structure sustainability.",
                "Evaluate a company's balance sheet under stress and analyze the divergence between Current, Quick, and Cash ratios."
            ),
            ["Data Analysis"] = (
                "Apply quantitative margin decomposition frameworks and unit-economic waterfall analysis.",
                "Perform a variance analysis decomposing product line gross margin declines into price, volume, and cost variances."
            )
        };

    public Task<RoadmapResponse> GenerateAsync(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleId = request.RoleId?.Trim().ToLowerInvariant() ?? string.Empty;
        var topGaps = request.TopGaps ?? new List<string>();

        // Strictly at most 3 priorities
        var prioritizedGaps = topGaps.Take(3).ToList();

        var guidance = roleId == "financial-analyst"
            ? FinancialAnalystGuidance
            : BackendDeveloperGuidance;

        var items = new List<RoadmapItemDto>();
        int priority = 1;

        foreach (var gap in prioritizedGaps)
        {
            if (guidance.TryGetValue(gap, out var details))
            {
                items.Add(new RoadmapItemDto(gap, priority, details.LearningGoal, details.PracticeTask));
            }
            else
            {
                // Fallback for custom or unmapped gap string
                items.Add(new RoadmapItemDto(
                    gap,
                    priority,
                    $"Master core engineering competencies and industry best practices for {gap}.",
                    $"Complete a targeted hands-on exercise demonstrating measurable technical improvement in {gap}."
                ));
            }

            priority++;
        }

        return Task.FromResult(new RoadmapResponse(roleId, items));
    }
}
