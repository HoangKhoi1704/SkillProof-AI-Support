using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicProjectRecommender : IProjectRecommender
{
    private static readonly Dictionary<string, (string Requirement, string Deliverable)> BackendDeveloperGapMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SQL / Database"] = (
                "Design a relational database schema with users, expenses, categories, and budgets; create foreign keys, composite indexes on (UserId, ExpenseDate), and optimize query execution plans using EXPLAIN.",
                "SQL schema DDL scripts, migration files, and an EXPLAIN execution plan analysis showing before/after index optimization."
            ),
            ["Testing"] = (
                "Implement an automated unit and integration test suite isolating third-party payment gateways with mocks and validating currency rounding, failure paths, and concurrency edge cases.",
                "Automated test suite (xUnit) with 80%+ code coverage across business rules, gateway error handling, and validation boundaries."
            ),
            ["System Design"] = (
                "Implement a distributed sliding-window counter rate limiter with Redis caching for category lookups, along with documented fail-open cache outage mitigation.",
                "System architecture data-flow diagram and rate-limiting middleware returning standard HTTP 429 and Retry-After headers."
            ),
            ["REST API"] = (
                "Implement RESTful resource endpoints supporting partial resource updates with JSON Merge Patch and optimistic concurrency control using ETags.",
                "Documented OpenAPI/Swagger endpoints supporting idempotent PUT and concurrency-controlled PATCH operations."
            ),
            ["Authentication"] = (
                "Implement stateless JWT authentication with RSA cryptographic signature verification and short-lived access tokens backed by refresh token rotation.",
                "JWT authentication middleware and token rotation service with revocation endpoints."
            ),
            ["Programming Fundamentals"] = (
                "Implement an asynchronous background batch export pipeline using IAsyncEnumerable and CancellationToken to process large expense exports with bounded memory.",
                "Asynchronous streaming pipeline processing 50k+ records with cancellation checkpoints."
            )
        };

    private static readonly Dictionary<string, (string Requirement, string Deliverable)> FinancialAnalystGapMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Financial Modeling"] = (
                "Build a dynamic 3-statement financial model linking operational SaaS revenue cohorts, gross margins, and working capital schedules.",
                "Auditable 3-statement financial model workbook with dynamic formula architecture and scenario toggles."
            ),
            ["Forecasting"] = (
                "Develop a 3-year baseline, bull, and bear cash flow forecast decomposing working capital deterioration and cash conversion cycle divergence.",
                "3-year monthly cash flow projection with working capital analysis bridge (DSO, DIO, DPO)."
            ),
            ["Data Analysis"] = (
                "Perform a quantitative gross margin variance analysis waterfall and construct an executive reporting summary decomposing product line margin changes.",
                "Unit-economic margin variance waterfall chart and executive KPI variance report."
            ),
            ["Financial Statements"] = (
                "Trace income statement net income adjustments through operating cash flow to balance sheet retained earnings and working capital.",
                "Integrated 3-statement cash flow reconciliation schedule."
            ),
            ["Excel / Spreadsheets"] = (
                "Implement advanced lookup formulas (XLOOKUP, INDEX/MATCH), dynamic arrays (FILTER, UNIQUE), and structured table references with error handling.",
                "Production-grade financial workbook with structured table references and IFERROR auditing."
            ),
            ["Ratio Analysis"] = (
                "Calculate liquidity, profitability, and leverage ratios, interpreting divergence between Current and Quick ratios under stress.",
                "Financial ratio analysis matrix with industry benchmark comparisons."
            )
        };

    public Task<ProjectRecommendationResponse> RecommendAsync(
        RecommendProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleId = request.RoleId?.Trim().ToLowerInvariant() ?? string.Empty;
        var topGaps = request.TopGaps?.Take(3).ToList() ?? new List<string>();

        if (roleId == "financial-analyst")
        {
            return Task.FromResult(BuildFinancialAnalystProject(topGaps));
        }

        return Task.FromResult(BuildBackendDeveloperProject(topGaps));
    }

    private static ProjectRecommendationResponse BuildBackendDeveloperProject(IReadOnlyList<string> topGaps)
    {
        var requirements = new List<ProjectRequirementDto>();
        var deliverables = new List<string>();

        foreach (var gap in topGaps)
        {
            if (BackendDeveloperGapMap.TryGetValue(gap, out var mapping))
            {
                requirements.Add(new ProjectRequirementDto(mapping.Requirement, gap, mapping.Deliverable));
                deliverables.Add(mapping.Deliverable);
            }
            else
            {
                var req = $"Implement core engineering requirements and industry best practices targeting {gap}.";
                var del = $"Production-quality implementation module and automated test verification for {gap}.";
                requirements.Add(new ProjectRequirementDto(req, gap, del));
                deliverables.Add(del);
            }
        }

        return new ProjectRecommendationResponse(
            Title: "Expense Management API",
            Description: "A production-grade RESTful expense tracking and budget management backend engineered specifically to build and demonstrate relational database indexing, automated test isolation, and distributed system resilience.",
            Reason: $"Directly addresses your diagnosed skill gaps ({string.Join(", ", topGaps)}) by forcing hands-on implementation of relational query optimization, automated test mocking, and distributed architecture patterns.",
            Requirements: requirements,
            ExpectedDeliverables: deliverables
        );
    }

    private static ProjectRecommendationResponse BuildFinancialAnalystProject(IReadOnlyList<string> topGaps)
    {
        var requirements = new List<ProjectRequirementDto>();
        var deliverables = new List<string>();

        foreach (var gap in topGaps)
        {
            if (FinancialAnalystGapMap.TryGetValue(gap, out var mapping))
            {
                requirements.Add(new ProjectRequirementDto(mapping.Requirement, gap, mapping.Deliverable));
                deliverables.Add(mapping.Deliverable);
            }
            else
            {
                var req = $"Construct financial analysis modules and analytical frameworks targeting {gap}.";
                var del = $"Financial analysis schedule and documentation demonstrating proficiency in {gap}.";
                requirements.Add(new ProjectRequirementDto(req, gap, del));
                deliverables.Add(del);
            }
        }

        return new ProjectRecommendationResponse(
            Title: "Company Financial Health & 3-Year Outlook",
            Description: "A comprehensive financial evaluation and multi-scenario forecast engineered specifically to build dynamic financial models, cash conversion cycle forecasts, and unit-economic margin variance analyses.",
            Reason: $"Directly addresses your diagnosed skill gaps ({string.Join(", ", topGaps)}) by requiring hands-on financial modeling, cash flow forecasting, and quantitative variance decomposition.",
            Requirements: requirements,
            ExpectedDeliverables: deliverables
        );
    }
}
