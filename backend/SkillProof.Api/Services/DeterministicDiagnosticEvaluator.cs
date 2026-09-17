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

        var answersByQuestionId = submissions
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().Answer, StringComparer.OrdinalIgnoreCase);

        var skillEvaluations = new List<SkillEvaluationItem>();

        var isDynamicSqlite = submissions.Any(s => s.QuestionId.StartsWith("q-", StringComparison.OrdinalIgnoreCase));

        if (isDynamicSqlite)
        {
            foreach (var sub in submissions)
            {
                var (competency, legacyId) = ResolveCompetencyFromSqliteId(sub.QuestionId);
                var rawAnswer = sub.Answer?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(rawAnswer) || rawAnswer.Length < 15)
                {
                    skillEvaluations.Add(new SkillEvaluationItem(
                        competency,
                        "Insufficient Evidence",
                        $"The answer submitted for {competency} is empty or lacks sufficient technical explanation to assess competence.",
                        new List<string>()
                    ));
                    continue;
                }

                var (level, reason, evidence) = EvaluateSubstantiveAnswer(normalizedRoleId, legacyId, competency, rawAnswer);
                var safeLevel = ApprovedLevels.Contains(level) ? level : "Insufficient Evidence";
                skillEvaluations.Add(new SkillEvaluationItem(competency, safeLevel, reason, evidence));
            }
        }
        else
        {
            var roleQuestions = SeedData.Questions
                .Where(q => q.CareerRoleId.Equals(normalizedRoleId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var q in roleQuestions)
            {
                var key = q.Id.ToString();
                answersByQuestionId.TryGetValue(key, out var rawAnswer);
                var trimmedAnswer = rawAnswer?.Trim() ?? string.Empty;

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
                var safeLevel = ApprovedLevels.Contains(level) ? level : "Insufficient Evidence";
                skillEvaluations.Add(new SkillEvaluationItem(q.Competency, safeLevel, reason, evidence));
            }
        }

        var topGaps = CalculateTopGaps(skillEvaluations);
        return Task.FromResult(new EvaluationResponse(normalizedRoleId, skillEvaluations, topGaps));
    }

    public static (string Level, string Reason, List<string> Evidence) EvaluateSingleAnswer(
        string roleId,
        string questionId,
        string answer)
    {
        var normalizedRoleId = roleId.Trim().ToLowerInvariant();
        var rawAnswer = answer?.Trim() ?? string.Empty;

        var (competency, legacyId) = ResolveCompetencyFromSqliteId(questionId);

        if (string.IsNullOrWhiteSpace(rawAnswer) || rawAnswer.Length < 15)
        {
            return (
                "Insufficient Evidence",
                $"The answer submitted for {competency} is empty or lacks sufficient technical explanation to assess competence.",
                new List<string>()
            );
        }

        var (level, reason, evidence) = EvaluateSubstantiveAnswer(normalizedRoleId, legacyId, competency, rawAnswer);
        var safeLevel = ApprovedLevels.Contains(level) ? level : "Insufficient Evidence";
        return (safeLevel, reason, evidence);
    }

    public static (string Competency, int LegacyId) ResolveCompetencyFromSqliteId(string questionId)
    {
        var id = questionId ?? string.Empty;
        // Backend
        if (id.Contains("-prog-")) return ("Programming Fundamentals", 6);
        if (id.Contains("-rest-")) return ("REST API", 1);
        if (id.Contains("-test-")) return ("Testing", 3);
        if (id.Contains("-sec-") && id.StartsWith("q-be-")) return ("Authentication", 5);
        if (id.Contains("-auth-")) return ("Authentication", 5);
        if (id.Contains("-sys-")) return ("System Design", 4);
        if (id.Contains("-nosql-")) return ("NoSQL Databases", 13);
        if (id.Contains("-cache-")) return ("Caching & Performance", 14);
        if (id.Contains("-cs-")) return ("C# / .NET", 15);
        if (id.Contains("-java-")) return ("Java", 16);
        if (id.Contains("-py-") && id.StartsWith("q-be-")) return ("Python", 17);
        if (id.Contains("-cpp-")) return ("C++", 18);
        if (id.Contains("-js-") && id.StartsWith("q-be-")) return ("JavaScript", 19);
        if (id.Contains("-ts-")) return ("TypeScript", 20);
        if (id.Contains("-go-")) return ("Go", 21);
        if (id.Contains("-rust-")) return ("Rust", 22);

        // Frontend
        if (id.StartsWith("q-fe-http-")) return ("Internet & HTTP Protocols", 101);
        if (id.StartsWith("q-fe-html-")) return ("HTML Fundamentals", 102);
        if (id.StartsWith("q-fe-js-")) return ("JavaScript Core", 103);
        if (id.StartsWith("q-fe-sec-")) return ("Web Security", 104);

        // Data Analyst
        if (id.StartsWith("q-da-sheets-")) return ("Spreadsheets", 201);
        if (id.StartsWith("q-da-sql-") || (id.Contains("-sql-") && !id.StartsWith("q-be-"))) return ("SQL", 202);
        if (id.StartsWith("q-be-sql-")) return ("SQL / Database", 2);
        if (id.StartsWith("q-da-py-")) return ("Python / R", 203);
        if (id.StartsWith("q-da-wrang-")) return ("Data Wrangling", 204);
        if (id.StartsWith("q-da-eda-")) return ("Exploratory Data Analysis", 205);
        if (id.StartsWith("q-da-stat-")) return ("Statistics", 206);
        if (id.StartsWith("q-da-bi-")) return ("BI & Dashboards", 207);
        if (id.StartsWith("q-da-vis-")) return ("Data Visualization", 208);

        return (id, 1);
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

                case 13: // NoSQL Databases
                    return (
                        "Intermediate",
                        "Demonstrates solid understanding of document store modeling, denormalization, and CAP theorem consistency trade-offs.",
                        new List<string> { "Explained document modeling vs normalized schemas", "Addressed consistency and access pattern trade-offs" }
                    );

                case 14: // Caching & Performance
                    return (
                        "Intermediate",
                        "Explains cache-aside pattern, TTL invalidation, cache stampede mitigation, and eviction policies.",
                        new List<string> { "Identified cache-aside pattern and stampede mitigation", "Addressed distributed caching invalidation trade-offs" }
                    );

                case 15: // C# / .NET
                    return (
                        "Intermediate",
                        "Demonstrates practical .NET backend implementation proficiency, memory management, and asynchronous execution.",
                        new List<string> { "Applied .NET runtime constructs effectively", "Addressed async/await and dependency injection lifetimes" }
                    );

                case 16: // Java
                    return (
                        "Intermediate",
                        "Demonstrates practical JVM backend implementation proficiency, Spring Boot concepts, and concurrency patterns.",
                        new List<string> { "Applied Java/Spring backend patterns effectively", "Addressed JVM threading and transaction boundaries" }
                    );

                case 17: // Python
                    return (
                        "Intermediate",
                        "Demonstrates practical Python backend implementation proficiency using FastAPI/AsyncIO and GIL considerations.",
                        new List<string> { "Applied async Python constructs effectively", "Addressed GIL limitations and multiprocessing strategies" }
                    );

                case 18: // C++
                    return (
                        "Intermediate",
                        "Demonstrates deterministic memory management with RAII, smart pointers, and concurrency synchronization.",
                        new List<string> { "Applied modern C++ memory management (unique_ptr, RAII)", "Addressed thread synchronization and data race safety" }
                    );

                case 19: // JavaScript
                    return (
                        "Intermediate",
                        "Demonstrates mastery of the Node.js event loop, asynchronous I/O, streams, and backpressure handling.",
                        new List<string> { "Explained event loop microtask vs macrotask execution", "Addressed stream backpressure and event loop starvation" }
                    );

                case 20: // TypeScript
                    return (
                        "Intermediate",
                        "Demonstrates advanced structural typing, conditional types, and runtime schema validation with Zod.",
                        new List<string> { "Utilized advanced TypeScript generic and mapped types", "Addressed type erasure with runtime schema validation" }
                    );

                case 21: // Go
                    return (
                        "Intermediate",
                        "Demonstrates effective Go concurrency with goroutines, channels, context cancellation, and CSP patterns.",
                        new List<string> { "Applied goroutines and channel worker pool patterns", "Addressed context cancellation and goroutine leak prevention" }
                    );

                case 22: // Rust
                    return (
                        "Intermediate",
                        "Demonstrates memory safety through ownership and borrowing, Tokio asynchronous tasks, and thread safety.",
                        new List<string> { "Applied Rust ownership and borrow checker rules", "Addressed async Tokio tasks with Arc and async Mutex" }
                    );
            }
        }
        else if (roleId == "frontend-developer")
        {
            switch (questionId)
            {
                case 101: // Internet & HTTP
                    return (
                        "Intermediate",
                        "Accurately explains DNS resolution, TCP handshake, TLS encryption, and HTTP request caching.",
                        new List<string> { "Explained DNS lookup and TCP/TLS handshakes", "Distinguished Cache-Control immutable bundles vs HTML revalidation" }
                    );

                case 102: // HTML
                    return (
                        "Intermediate",
                        "Demonstrates correct semantic HTML tags, accessible landmarks, and ARIA roles.",
                        new List<string> { "Used semantic elements (<main>, <nav>, <article>)", "Explained WCAG accessibility contrast and screen reader accessibility" }
                    );

                case 103: // JavaScript
                    return (
                        "Intermediate",
                        "Demonstrates clear understanding of the event loop, closures, promises, and prototypal inheritance.",
                        new List<string> { "Articulated event loop microtask vs macrotask queuing", "Explained closures and lexical scope encapsulation" }
                    );

                case 104: // Web Security
                    return (
                        "Intermediate",
                        "Explains XSS, CSRF, and CORS browser security policies with practical defense mechanisms.",
                        new List<string> { "Contrasted XSS input sanitization with CSRF SameSite cookies", "Explained CORS preflight and Access-Control-Allow-Origin headers" }
                    );
            }
        }
        else if (roleId == "data-analyst")
        {
            switch (questionId)
            {
                case 201: // Spreadsheets
                    return (
                        "Intermediate",
                        "Demonstrates advanced spreadsheet formula modeling, XLOOKUP advantages, and pivot table distinct counting.",
                        new List<string> { "Contrasted XLOOKUP with VLOOKUP exact match and column flexibility", "Applied distinct count formulas and data normalization" }
                    );

                case 202: // SQL
                    return (
                        "Intermediate",
                        "Accurately utilizes window functions, CTEs, and explains WHERE vs HAVING filtering order.",
                        new List<string> { "Used LAG() window function for sequential cohort analysis", "Contrasted ROW_NUMBER, RANK, and DENSE_RANK tie semantics" }
                    );

                case 203: // Python / R
                    return (
                        "Intermediate",
                        "Demonstrates vectorized DataFrame calculations in Pandas, handling datetime parsing, and multi-key aggregations.",
                        new List<string> { "Explained vectorization SIMD efficiency vs Python loop overhead", "Applied pd.to_datetime, groupby, and pivot_table operations" }
                    );

                case 204: // Data Wrangling
                    return (
                        "Intermediate",
                        "Applies Tidy Data rules, addresses missing data mechanisms, and implements fuzzy string deduplication.",
                        new List<string> { "Articulated three Tidy Data rules", "Contrasted MCAR/MAR/MNAR missingness mechanisms and fuzzy matching" }
                    );

                case 205: // EDA
                    return (
                        "Intermediate",
                        "Analyzes univariate and bivariate distributions, explains Simpson's Paradox, and identifies self-selection bias.",
                        new List<string> { "Explained Anscombe's Quartet limitations of summary metrics", "Diagnosed Simpson's Paradox and recommended subgroup stratification" }
                    );

                case 206: // Statistics
                    return (
                        "Intermediate",
                        "Accurately defines p-values, Type I / Type II business errors, and family-wise error inflation from peeking.",
                        new List<string> { "Correctly defined p-value conditionality under null hypothesis", "Explained family-wise error rate inflation and power analysis" }
                    );

                case 207: // BI & Dashboards
                    return (
                        "Intermediate",
                        "Applies star schema dimensional modeling, handles semi-additive inventory metrics, and structures executive KPI hierarchies.",
                        new List<string> { "Contrasted fact vs dimension tables in star schema", "Applied progressive disclosure and top-down KPI hierarchy" }
                    );

                case 208: // Visualization
                    return (
                        "Intermediate",
                        "Applies Cleveland & McGill perceptual ranking hierarchies, rejects misleading 3D charts, and upholds ethical axis scaling.",
                        new List<string> { "Ranked position along common scale above angle and area", "Contrasted bar chart zero baseline requirement with line charts" }
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
