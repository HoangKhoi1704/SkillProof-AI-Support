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

    private static readonly Dictionary<string, (string Requirement, string Deliverable, string Criterion)> AssessableSkillRequirementMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["sql"] = (
                "Design a relational data model with foreign keys, composite indexes, and implement transaction concurrency isolation with EXPLAIN query plan analysis.",
                "SQL DDL schema, migration scripts, and an EXPLAIN execution plan analysis showing before/after index optimization.",
                "Demonstrated understanding of transaction boundaries, isolation levels, index structures, and query execution plans."
            ),
            ["testing"] = (
                "Implement an automated unit and integration test suite isolating third-party boundaries with test doubles/mocks and validating error handling and concurrency race conditions.",
                "Automated test suite (xUnit/pytest/jest) with high branch coverage across business invariants, boundary conditions, and mock assertions.",
                "Evidence of comprehensive test isolation, dependency mocking, and boundary assertion testing."
            ),
            ["system-design"] = (
                "Design and implement a distributed rate limiter or caching strategy with graceful degradation and documented failure mitigation.",
                "System architecture data-flow diagram and rate-limiting/caching middleware returning standard HTTP status codes and headers.",
                "Demonstrated architectural reasoning for distributed state, failure isolation, and resilience trade-offs."
            ),
            ["rest-api"] = (
                "Design and implement RESTful resource endpoints with idempotency keys, ETag optimistic concurrency control, and standard error handling.",
                "OpenAPI/Swagger documentation and endpoint implementations supporting idempotent PUT and concurrency-controlled PATCH operations.",
                "Evidence of robust HTTP contract semantics, idempotency guarantees, and status code consistency."
            ),
            ["authentication-security"] = (
                "Implement stateless JWT authentication with cryptographic signature verification, refresh token rotation, and role-based access control.",
                "Security middleware, token issuance service, and revocation/rotation endpoints with secure password hashing.",
                "Demonstrated cryptographic verification, token rotation mechanics, and principle of least privilege."
            ),
            ["programming-fundamentals"] = (
                "Implement an asynchronous data processing pipeline with bounded memory consumption and cooperative cancellation token propagation.",
                "Asynchronous streaming pipeline implementation with bounded memory benchmarks and cancellation handling.",
                "Demonstrated proficiency in asynchronous programming, resource management, and error propagation."
            ),
            ["nosql"] = (
                "Design a document or key-value data model with TTL expiration, secondary indexing, and eventual consistency handling.",
                "NoSQL schema definition, document repository implementation, and consistency trade-off documentation.",
                "Demonstrated data modeling principles for non-relational stores, partition keys, and consistency semantics."
            ),
            ["caching"] = (
                "Implement a multi-tier cache-aside pattern with TTL expiration, cache stampede mitigation, and explicit invalidation hooks.",
                "Caching service wrapper, cache hit/miss telemetry, and documented cache invalidation strategies.",
                "Demonstrated understanding of cache coherence, TTL strategies, and stampede prevention."
            ),
            ["csharp"] = (
                "Implement idiomatic C# domain services utilizing nullable reference types, records, pattern matching, and Task-based asynchronous patterns.",
                "C# service implementation with clean solution architecture and async/await task orchestration.",
                "Demonstrated idiomatic C# syntax, type safety, and thread-safe async patterns."
            ),
            ["java"] = (
                "Implement idiomatic Java service components utilizing modern stream APIs, records, CompletableFuture, and Spring/Jakarta annotations.",
                "Java service implementation with modular architecture and exception handling hierarchy.",
                "Demonstrated modern Java features, memory considerations, and concurrent task management."
            ),
            ["python"] = (
                "Implement idiomatic Python backend services utilizing type hints, dataclasses/Pydantic models, and asyncio coroutines.",
                "Python service implementation with strict type annotation coverage and async I/O orchestration.",
                "Demonstrated idiomatic Pythonic design, type hints, and asynchronous coroutine management."
            ),
            ["cpp"] = (
                "Implement high-performance C++ backend components utilizing RAII, smart pointers, move semantics, and std::jthread concurrency.",
                "Modern C++20 implementation with zero-copy data pipelines and RAII lifetime management.",
                "Demonstrated memory safety via RAII, move semantics, and modern standard library concurrency."
            ),
            ["javascript"] = (
                "Implement asynchronous Node.js backend services leveraging EventLoop-friendly non-blocking I/O and structured error hierarchies.",
                "Node.js module implementation with structured logging, async error middleware, and event stream handling.",
                "Demonstrated non-blocking event-driven architecture and asynchronous error propagation."
            ),
            ["typescript"] = (
                "Implement strongly-typed backend modules utilizing TypeScript generics, utility types, discriminated unions, and runtime schema validation.",
                "TypeScript service implementation with strict compiler options and Zod/TypeBox validation pipelines.",
                "Demonstrated advanced type modeling, discriminated unions, and runtime validation."
            ),
            ["go"] = (
                "Implement idiomatic Go services utilizing goroutines, buffered channels, context cancellation propagation, and explicit error handling.",
                "Go package implementation with concurrency coordination, context timeouts, and benchmark tests.",
                "Demonstrated idiomatic Go conventions, race-free channel communication, and context propagation."
            ),
            ["rust"] = (
                "Implement memory-safe Rust backend services utilizing ownership semantics, custom error types with thiserror/anyhow, and Tokio async runtimes.",
                "Rust crate implementation with strict borrow-checker safety and async Tokio task spawning.",
                "Demonstrated ownership/borrowing mastery, fearless concurrency, and algebraic error handling."
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

    public Task<GapBasedProjectDto> RecommendGapBasedAsync(
        ProjectGapContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        var roleId = context.RoleId?.Trim().ToLowerInvariant() ?? "backend-developer";

        var targetSkills = context.TargetSkills ?? new List<ProjectGapTargetSkill>();
        var targetedSkillDtos = new List<TargetedSkillDto>();
        var requirements = new List<ProjectRequirementDto>();
        var deliverables = new List<string>();
        var criteria = new List<string>();

        foreach (var target in targetSkills)
        {
            var skillKey = target.SkillId.Trim().ToLowerInvariant();
            var whyIncluded = $"Directly addresses your diagnosed {target.CurrentLevel} level in {target.SkillId}, focusing on {target.TargetArea}.";
            targetedSkillDtos.Add(new TargetedSkillDto(target.SkillId, target.CurrentLevel, target.TargetArea, whyIncluded));

            if (AssessableSkillRequirementMap.TryGetValue(skillKey, out var mapping))
            {
                var req = new ProjectRequirementDto(mapping.Requirement, target.SkillId, mapping.Deliverable, target.SkillId);
                requirements.Add(req);
                deliverables.Add(mapping.Deliverable);
                criteria.Add(mapping.Criterion);
            }
            else
            {
                var reqText = !string.IsNullOrWhiteSpace(target.PracticeTask)
                    ? target.PracticeTask
                    : $"Implement core engineering requirements and industry best practices targeting {target.TargetArea}.";
                var delivText = !string.IsNullOrWhiteSpace(target.EvidenceTarget)
                    ? target.EvidenceTarget
                    : $"Production-quality implementation module and automated verification for {target.SkillId}.";
                var critText = $"Demonstrated technical capability and trade-off reasoning in {target.TargetArea}.";

                var req = new ProjectRequirementDto(reqText, target.SkillId, delivText, target.SkillId);
                requirements.Add(req);
                deliverables.Add(delivText);
                criteria.Add(critText);
            }
        }

        // Keep project scope bounded: 3-6 core requirements
        if (requirements.Count > 6)
        {
            requirements = requirements.Take(6).ToList();
            deliverables = deliverables.Take(6).ToList();
            criteria = criteria.Take(6).ToList();
        }

        var evidenceRequirements = new List<string>
        {
            "Working source code repository or code excerpt demonstrating modular architecture and adherence to requirements.",
            "Automated test suite (unit and integration tests) verifying business logic and failure edge cases.",
            "Architecture decision explanation detailing concurrency, data modeling, or failure isolation trade-offs."
        };

        string title, scenario, objective, portfolioOutcome;

        if (roleId == "financial-analyst")
        {
            title = "Integrated Corporate Financial Model & Multi-Scenario Forecast";
            scenario = "An executive leadership team requires a robust, dynamic 3-statement financial model and liquidity forecast to evaluate strategic expansion scenarios and capital allocation under volatile market conditions.";
            objective = "Construct an auditable, dynamic financial model decomposing unit economics, working capital dynamics, and cash flow projections addressing diagnosed analytical gaps.";
            portfolioOutcome = "A professional corporate financial workbook with dynamic scenario bridges, variance waterfall schedules, and an executive briefing deck suitable for investment committee review.";
        }
        else
        {
            title = "Production-Grade Resilient Order & Inventory Service";
            scenario = "A high-volume transactional ordering and inventory service requires robust concurrency controls, reliable API contracts, comprehensive test automation, and resilient data access to eliminate overselling anomalies and downtime.";
            objective = "Engineer an end-to-end backend service demonstrating rigorous transaction isolation, automated test coverage, and scalable architecture directly addressing diagnosed competency gaps.";
            portfolioOutcome = "A production-grade, containerizable backend API with documented architectural decision records (ADRs), complete automated test suite, and query plan benchmarks demonstrating readiness for mid-level backend engineering.";
        }

        var project = new GapBasedProjectDto(
            ProjectId: "proj-" + Guid.NewGuid().ToString("N")[..8],
            RoleId: roleId,
            Title: title,
            Scenario: scenario,
            Objective: objective,
            TargetedSkills: targetedSkillDtos,
            Requirements: requirements,
            Deliverables: deliverables,
            EvidenceRequirements: evidenceRequirements,
            EvaluationCriteria: criteria,
            PortfolioOutcome: portfolioOutcome
        );

        return Task.FromResult(project);
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
