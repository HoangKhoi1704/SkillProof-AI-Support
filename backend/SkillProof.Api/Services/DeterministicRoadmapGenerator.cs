using SkillProof.Api.Models;

namespace SkillProof.Api.Services;

public class DeterministicRoadmapGenerator : IRoadmapGenerator
{
    private record SkillDetailTemplate(
        string TargetArea,
        string LearningGoal,
        List<string> LearningActions,
        string PracticeTask,
        string EvidenceTarget,
        List<string> CompletionCriteria
    );

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

    // Milestone I8 Rich Domain Guidance for Assessable Skills
    private static readonly Dictionary<string, (SkillDetailTemplate Dev, SkillDetailTemplate Evid)> SkillCatalogGuidance =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["sql"] = (
                new SkillDetailTemplate(
                    TargetArea: "Transaction Isolation & Concurrency Reasoning",
                    LearningGoal: "Reason about transaction isolation levels, locking mechanisms, and concurrency anomaly mitigation under high contention.",
                    LearningActions: new() { "Study ACID isolation levels (Read Committed vs Repeatable Read vs Serializable)", "Analyze shared vs exclusive locks and deadlock avoidance", "Evaluate MVCC behavior and snapshot isolation under concurrent writes" },
                    PracticeTask: "Analyze a high-concurrency payment ledger schema, simulate concurrent balance deductions, reproduce a race condition, and implement row-level locking using SELECT FOR UPDATE with transaction rollbacks.",
                    EvidenceTarget: "A written technical decision document and runnable test suite verifying isolation behavior and concurrency anomaly mitigation.",
                    CompletionCriteria: new() { "Evaluates transaction isolation levels under concurrent writes", "Prevents lost updates and race conditions under test load", "Documents locking mechanics and throughput trade-offs" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Query Execution Plan & Indexing Diagnostic",
                    LearningGoal: "Produce verifiable evidence of query execution analysis, index evaluation, and relational database reasoning.",
                    LearningActions: new() { "Inspect slow-query logs and execution plans using EXPLAIN ANALYZE", "Identify sequential table scan bottlenecks versus index scans", "Formulate indexing hypotheses against specific query access patterns" },
                    PracticeTask: "Investigate a realistic slow-query incident on an orders schema: analyze the EXPLAIN execution plan, identify the sequential scan bottleneck, propose a composite index, and benchmark query latency before and after.",
                    EvidenceTarget: "A concise technical analysis report containing before-and-after query execution plans and index trade-off evaluation.",
                    CompletionCriteria: new() { "Demonstrates observable query plan interpretation", "Explains index selection rationale clearly", "Produces measurable latency reduction evidence" }
                )
            ),
            ["testing"] = (
                new SkillDetailTemplate(
                    TargetArea: "Integration Test Design & Fault Isolation",
                    LearningGoal: "Master integration test design, realistic test doubles, and fault isolation in distributed service boundaries.",
                    LearningActions: new() { "Study boundary test doubles and wiremock patterns", "Analyze transient network timeout simulation", "Evaluate idempotent retry testing strategies" },
                    PracticeTask: "Construct an automated integration test suite for an external payment gateway simulating transient socket timeouts, idempotency key replays, and signature validation failures without leaking external dependencies.",
                    EvidenceTarget: "A runnable automated test suite with deterministic failure assertions and test isolation documentation.",
                    CompletionCriteria: new() { "Verifies failure handling without external network dependencies", "Validates idempotency and retry semantics deterministically", "Ensures test suite independence and repeatable execution" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Unit Test Structuring & Boundary Verification",
                    LearningGoal: "Produce verifiable evidence of automated test structuring, boundary verification, and regression prevention.",
                    LearningActions: new() { "Review test boundary definitions and assertion patterns", "Analyze edge case identification in core business logic", "Structure isolated test cases with mock dependencies" },
                    PracticeTask: "Author an automated test suite for an order discount calculation engine, covering null inputs, negative values, tiered discounts, and boundary conditions with clear arrange-act-assert structuring.",
                    EvidenceTarget: "A pull-request artifact containing the unit test suite and a short commentary explaining edge-case selection.",
                    CompletionCriteria: new() { "Demonstrates clear test structuring and naming conventions", "Validates positive, negative, and edge-case branches", "Explains mock boundary selection rationale" }
                )
            ),
            ["system-design"] = (
                new SkillDetailTemplate(
                    TargetArea: "Distributed Rate Limiting & Partition Tolerance",
                    LearningGoal: "Reason about distributed state synchronization, partition tolerance, and cache stampede mitigation under burst traffic.",
                    LearningActions: new() { "Analyze sliding-window rate limiting algorithms", "Study cache stampede mitigation using single-flight/probabilistic early expiration", "Evaluate graceful degradation under downstream outage" },
                    PracticeTask: "Design and implement a distributed sliding-window rate limiter with Redis backing, returning standard HTTP 429 Retry-After headers, and specify fallback behavior when Redis is unreachable.",
                    EvidenceTarget: "An Architecture Decision Record (ADR) detailing rate-limiter topology, partition trade-offs, and failure recovery modes.",
                    CompletionCriteria: new() { "Specifies distributed rate-limiting algorithm and data structures", "Defines deterministic failure behavior when dependency fails", "Quantifies memory and latency trade-offs" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "High-Level Architecture & Scalability Modeling",
                    LearningGoal: "Produce verifiable evidence of distributed system trade-off analysis, bottleneck identification, and capacity estimation.",
                    LearningActions: new() { "Review core distributed principles: caching, partitioning, load balancing", "Analyze traffic estimation and read/write ratio calculations", "Formulate component architecture diagrams" },
                    PracticeTask: "Develop a high-level system architecture for a notification dispatch service handling 10k requests/second: define API gateways, asynchronous message queues, workers, and dead-letter queues with clear component boundaries.",
                    EvidenceTarget: "A technical design specification document with component architecture diagrams and capacity estimation calculations.",
                    CompletionCriteria: new() { "Produces clear component architecture and data flow diagrams", "Justifies asynchronous queue selection for decoupling", "Documents capacity and throughput assumptions" }
                )
            ),
            ["rest-api"] = (
                new SkillDetailTemplate(
                    TargetArea: "Idempotent Mutation & Concurrency Control",
                    LearningGoal: "Master HTTP specification semantics, conditional requests with ETags, and partial resource updates.",
                    LearningActions: new() { "Study RFC 7232 conditional requests and If-Match headers", "Analyze JSON Merge Patch (RFC 7386) vs JSON Patch (RFC 6902)", "Evaluate optimistic concurrency control patterns" },
                    PracticeTask: "Build an API endpoint supporting JSON Merge Patch with optimistic concurrency verification using ETag and If-Match headers, returning HTTP 412 Precondition Failed on conflicting updates.",
                    EvidenceTarget: "A runnable API controller implementation with automated tests verifying RFC compliance and conflict detection.",
                    CompletionCriteria: new() { "Implements proper HTTP status codes and conditional headers", "Handles concurrent update conflicts without data corruption", "Documents API contract and error response schemas" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "RESTful Resource Modeling & Error Contracts",
                    LearningGoal: "Produce verifiable evidence of standard HTTP semantics, REST resource modeling, and error handling.",
                    LearningActions: new() { "Review REST resource naming and URI hierarchy", "Analyze HTTP method semantics (GET, POST, PUT, DELETE)", "Standardize error responses adhering to RFC 7807 Problem Details" },
                    PracticeTask: "Design and implement a resource management REST API with CRUD operations, proper HTTP status codes (200, 201, 204, 400, 404), query pagination, and RFC 7807 compliant error payloads.",
                    EvidenceTarget: "An OpenAPI specification accompanied by working API endpoint code and request/response samples.",
                    CompletionCriteria: new() { "Applies standard HTTP verb semantics consistently", "Structures RFC 7807 Problem Details for all validation errors", "Implements clean pagination and resource modeling" }
                )
            ),
            ["authentication-security"] = (
                new SkillDetailTemplate(
                    TargetArea: "Token Revocation & Cryptographic Verification",
                    LearningGoal: "Master asymmetric cryptographic token verification, sliding refresh token rotation, and replay attack prevention.",
                    LearningActions: new() { "Analyze RS256/ECDSA asymmetric signature verification", "Study refresh token family rotation and reuse detection", "Review distributed token revocation strategies" },
                    PracticeTask: "Implement a secure authentication middleware that verifies RS256 JWTs against a JWKS endpoint, validates audience/issuer claims, and executes refresh token rotation with immediate revocation upon reuse detection.",
                    EvidenceTarget: "A security architecture document and code implementation verifying token validation and replay mitigation.",
                    CompletionCriteria: new() { "Enforces asymmetric signature and claim validation", "Detects and revokes compromised refresh token families", "Secures secret management and credential transmission" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Authentication & Password Hashing Fundamentals",
                    LearningGoal: "Produce verifiable evidence of secure password hashing, token-based authentication, and role-based access control.",
                    LearningActions: new() { "Review modern hashing standards (Argon2, bcrypt)", "Analyze JWT payload structure and stateless claims", "Define role-based authorization policies" },
                    PracticeTask: "Build an authentication service that securely registers users with Argon2id password hashing, issues short-lived JWT access tokens upon login, and protects an endpoint using role-based policy enforcement.",
                    EvidenceTarget: "A technical implementation with unit tests validating password verification and unauthorized access prevention.",
                    CompletionCriteria: new() { "Applies industry-standard password hashing parameters", "Constructs valid JWT claims without sensitive credential leakage", "Enforces access control returning HTTP 401/403 appropriately" }
                )
            ),
            ["programming-fundamentals"] = (
                new SkillDetailTemplate(
                    TargetArea: "Asynchronous Pipelines & Bounded Concurrency",
                    LearningGoal: "Master asynchronous streaming, bounded memory consumption, and non-blocking cancellation propagation.",
                    LearningActions: new() { "Study asynchronous stream mechanics and backpressure", "Analyze bounded concurrency with worker pools", "Review cancellation token forwarding across call stacks" },
                    PracticeTask: "Implement an asynchronous background batch processing pipeline that ingests a streaming data source, processes records with bounded concurrency of 5 workers, and handles cancellation signals gracefully without memory leaks.",
                    EvidenceTarget: "A runnable benchmarked code module demonstrating bounded memory usage and deterministic cancellation handling.",
                    CompletionCriteria: new() { "Enforces bounded parallelism preventing thread-pool exhaustion", "Maintains constant memory footprint under large data streams", "Propagates cancellation deterministically to all in-flight tasks" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Algorithmic Efficiency & Data Structures",
                    LearningGoal: "Produce verifiable evidence of core algorithmic thinking, time/space complexity analysis, and clean code structure.",
                    LearningActions: new() { "Review time/space complexity analysis (Big-O)", "Analyze efficient collection choices for lookup and traversal", "Structure clean, modular, and maintainable functions" },
                    PracticeTask: "Implement an efficient in-memory LRU cache data structure supporting O(1) get and put operations using a doubly linked list and hash map, accompanied by comprehensive unit tests.",
                    EvidenceTarget: "A well-documented code repository with time and space complexity analysis and unit test suite.",
                    CompletionCriteria: new() { "Demonstrates O(1) operational complexity for key operations", "Maintains invariant integrity across eviction boundaries", "Includes comprehensive tests validating edge conditions" }
                )
            ),
            ["caching"] = (
                new SkillDetailTemplate(
                    TargetArea: "Cache Invalidation & Stampede Mitigation",
                    LearningGoal: "Master cache-aside patterns, write-through vs write-back semantics, and probabilistic early expiration.",
                    LearningActions: new() { "Analyze cache-aside invalidation vs TTL trade-offs", "Study single-flight pattern and distributed locking for stampede mitigation", "Evaluate multi-tier L1/L2 caching topologies" },
                    PracticeTask: "Implement a robust cache-aside layer over a database query using Redis with a mutex lock to prevent cache stampede when keys expire, and write a benchmark test simulating 100 concurrent requests during key expiration.",
                    EvidenceTarget: "A technical report with benchmark graphs comparing cache-hit latencies and database query counts during key expiry.",
                    CompletionCriteria: new() { "Demonstrates stampede prevention under high concurrent load", "Ensures cache consistency upon database record updates", "Measures and documents latency reduction metrics" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Caching Patterns & Key Expiration Diagnostics",
                    LearningGoal: "Produce verifiable evidence of caching strategy selection, key naming conventions, and TTL management.",
                    LearningActions: new() { "Review cache-aside workflow and read-through caching", "Analyze cache key design and collision prevention", "Formulate sensible TTL policies based on data mutability" },
                    PracticeTask: "Implement an in-memory caching wrapper for an external API, applying structured cache keys, appropriate TTL expiration, and graceful fallback when the upstream service times out.",
                    EvidenceTarget: "Code implementation with unit tests validating cache hits, cache misses, and expiration behavior.",
                    CompletionCriteria: new() { "Applies clear cache key naming conventions", "Handles upstream service failures gracefully from cached state", "Validates TTL expiration mechanics in automated tests" }
                )
            ),
            ["nosql"] = (
                new SkillDetailTemplate(
                    TargetArea: "Partition Key Cardinality & Hot-Partition Mitigation",
                    LearningGoal: "Master document and wide-column data modeling, access-pattern-driven schema design, and partition key distribution.",
                    LearningActions: new() { "Analyze query-driven schema modeling vs relational normalization", "Study partition key cardinality and hot-partition mitigation", "Evaluate eventual consistency and read/write trade-offs" },
                    PracticeTask: "Design a NoSQL document schema for an e-commerce catalog with dynamic attributes, select an optimal partition key to prevent hot partitions, and demonstrate querying without cross-partition fanout.",
                    EvidenceTarget: "A data modeling technical document detailing access patterns, partition strategy, and indexing configuration.",
                    CompletionCriteria: new() { "Aligns document schema directly with high-frequency query patterns", "Prevents hot partition bottlenecks through high-cardinality keys", "Documents eventual consistency implications for read operations" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Document Querying & Indexing Diagnostics",
                    LearningGoal: "Produce verifiable evidence of NoSQL query construction, schema flexibility, and document lifecycle management.",
                    LearningActions: new() { "Review JSON document structures and embedding vs referencing", "Analyze filtering and indexing in document stores", "Formulate CRUD operations with document collections" },
                    PracticeTask: "Build a document repository service that performs CRUD operations, compound index queries, and aggregation pipeline filtering against a collection of customer activity logs.",
                    EvidenceTarget: "A working code repository with integration tests validating document insertion, querying, and index usage.",
                    CompletionCriteria: new() { "Demonstrates proficient document querying and filtering", "Applies appropriate embedding vs referencing decisions", "Validates query efficiency with compound indexes" }
                )
            ),
            ["python"] = (
                new SkillDetailTemplate(
                    TargetArea: "Asyncio Concurrency & GIL Optimization",
                    LearningGoal: "Master Python asynchronous concurrency, GIL bottleneck mitigation, generator pipelines, and static type safety.",
                    LearningActions: new() { "Study asyncio task scheduling and event loop blocking hazards", "Analyze multiprocessing versus threading trade-offs under the GIL", "Leverage advanced type annotations (typing.Protocol, TypeVar)" },
                    PracticeTask: "Implement a high-throughput asynchronous web scraper using aiohttp with bounded task concurrency and backpressure, streaming parsed records through a memory-efficient generator pipeline.",
                    EvidenceTarget: "A benchmarked Python package with asynchronous unit tests and type verification using mypy in strict mode.",
                    CompletionCriteria: new() { "Prevents event-loop blocking using non-blocking asynchronous primitives", "Implements memory-efficient stream generation with type hints", "Passes strict static type analysis" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Idiomatic Python & Core Data Structures",
                    LearningGoal: "Produce verifiable evidence of idiomatic Python syntax, standard library mastery, and exception handling.",
                    LearningActions: new() { "Review Pythonic idioms (comprehensions, context managers)", "Analyze standard library collections (deque, defaultdict, Counter)", "Structure robust exception hierarchies" },
                    PracticeTask: "Author a command-line log analysis utility in Python that parses structured log files using context managers, calculates error rates using collections.Counter, and handles malformed lines gracefully.",
                    EvidenceTarget: "A clean Python module with pytest suite validating parsing accuracy and edge-case handling.",
                    CompletionCriteria: new() { "Uses idiomatic Pythonic constructs and context managers", "Applies appropriate standard library collections", "Handles file I/O errors and malformed inputs cleanly" }
                )
            ),
            ["csharp"] = (
                new SkillDetailTemplate(
                    TargetArea: "Zero-Allocation Memory & Asynchronous Streams",
                    LearningGoal: "Master modern .NET performance engineering, Span/Memory allocation reduction, and IAsyncEnumerable pipelines.",
                    LearningActions: new() { "Study Span<T> and ReadOnlySpan<T> for allocation-free parsing", "Analyze ValueTask vs Task throughput trade-offs", "Implement asynchronous streaming with IAsyncEnumerable" },
                    PracticeTask: "Implement a high-performance CSV log parser using ReadOnlySpan<char> and IAsyncEnumerable<LogEntry>, demonstrating zero heap allocations in the hot parsing path under benchmark tests.",
                    EvidenceTarget: "A BenchmarkDotNet benchmark suite and C# implementation proving reduced GC allocation pressure.",
                    CompletionCriteria: new() { "Demonstrates zero allocation in the hot string parsing path", "Utilizes IAsyncEnumerable for non-blocking stream processing", "Validates throughput gains with BenchmarkDotNet" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Idiomatic C# & Dependency Injection Fundamentals",
                    LearningGoal: "Produce verifiable evidence of clean object-oriented C#, LINQ query efficiency, and Microsoft.Extensions.DependencyInjection.",
                    LearningActions: new() { "Review C# pattern matching and record types", "Analyze LINQ deferred execution and allocation overhead", "Configure service lifetimes (Transient, Scoped, Singleton)" },
                    PracticeTask: "Build a modular notification dispatch library in C# configured via dependency injection, utilizing records and pattern matching for event dispatching with thorough xUnit test coverage.",
                    EvidenceTarget: "A clean .NET class library with comprehensive xUnit tests validating DI registration and event dispatching.",
                    CompletionCriteria: new() { "Applies modern C# language features and immutable records", "Configures service lifetimes correctly in DI container", "Includes xUnit tests with high assertion coverage" }
                )
            ),
            ["typescript"] = (
                new SkillDetailTemplate(
                    TargetArea: "Advanced Type Gymnastics & Conditional Types",
                    LearningGoal: "Master TypeScript conditional types, mapped types, template literal types, and compile-time type validation.",
                    LearningActions: new() { "Study conditional types and infer keyword mechanics", "Analyze mapped types and utility type construction", "Implement template literal types for type-safe routing" },
                    PracticeTask: "Construct a type-safe event bus library in TypeScript where event handlers are strictly mapped to their corresponding payload types at compile time using conditional and mapped types.",
                    EvidenceTarget: "A TypeScript package with compile-time type tests (dtslint or tsd) proving strict type enforcement.",
                    CompletionCriteria: new() { "Enforces strict compile-time payload matching without any types", "Constructs reusable utility types with template literal syntax", "Validates type-level assertions in automated test suite" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "TypeScript Interface Modeling & Strict Typing",
                    LearningGoal: "Produce verifiable evidence of clean interface modeling, strict null checks, and type guards.",
                    LearningActions: new() { "Review interface vs type alias design considerations", "Analyze user-defined type guards and narrowing", "Configure strict compiler flags (noImplicitAny, strictNullChecks)" },
                    PracticeTask: "Build an API response parsing module that safely parses unknown JSON payloads, validates schemas using user-defined type guards, and narrows types to strongly-typed domain interfaces.",
                    EvidenceTarget: "A strongly-typed TypeScript module with automated unit tests demonstrating safe runtime type narrowing.",
                    CompletionCriteria: new() { "Avoids explicit or implicit any assertions", "Implements custom type guards for safe runtime validation", "Compiles cleanly under strict TypeScript configuration" }
                )
            ),
            ["javascript"] = (
                new SkillDetailTemplate(
                    TargetArea: "Event Loop Mechanics & Memory Management",
                    LearningGoal: "Master the JavaScript event loop, microtask queues, closure memory leaks, and asynchronous promise orchestration.",
                    LearningActions: new() { "Analyze microtask (Promises) vs macrotask (setTimeout) scheduling", "Study closure retain cycles and heap profiling", "Evaluate Promise.allSettled vs Promise.race concurrency" },
                    PracticeTask: "Implement an asynchronous task queue with concurrency limits and retry policies, instrumented to demonstrate event loop non-blocking behavior and proper closure garbage collection.",
                    EvidenceTarget: "A Node.js module with unit tests demonstrating controlled concurrency and event loop sequencing.",
                    CompletionCriteria: new() { "Correctly sequences microtasks and asynchronous callbacks", "Prevents memory leaks in long-running queue closures", "Handles rejected promises deterministically" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Modern ES6+ Core Principles",
                    LearningGoal: "Produce verifiable evidence of modern JavaScript syntax, destructuring, promises, and functional array operations.",
                    LearningActions: new() { "Review ES6+ features (destructuring, spread, modules)", "Analyze Promise-based asynchronous control flows", "Leverage immutable array transformations (map, filter, reduce)" },
                    PracticeTask: "Build an in-memory data processing pipeline in JavaScript that transforms complex nested customer order data using functional array methods, with full Jest test coverage.",
                    EvidenceTarget: "A JavaScript repository with Jest tests validating functional transformation correctness.",
                    CompletionCriteria: new() { "Uses modern ES6+ syntax and module structuring", "Applies immutable functional transformations cleanly", "Validates edge cases with automated tests" }
                )
            ),
            ["go"] = (
                new SkillDetailTemplate(
                    TargetArea: "Goroutine Concurrency & Context Propagation",
                    LearningGoal: "Master Go concurrency primitives, channels, sync.WaitGroup, context cancellation, and race condition detection.",
                    LearningActions: new() { "Study unbuffered vs buffered channel synchronization", "Analyze context.WithTimeout propagation across HTTP boundaries", "Run Go race detector under concurrent test loads" },
                    PracticeTask: "Build a concurrent worker pool in Go that processes incoming jobs over buffered channels, coordinates shutdown with context cancellation, and passes tests with the -race flag enabled.",
                    EvidenceTarget: "A Go package with concurrent tests verifying zero race conditions via go test -race.",
                    CompletionCriteria: new() { "Passes go test -race without data race warnings", "Propagates context cancellation gracefully to all workers", "Prevents goroutine leaks upon worker termination" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Idiomatic Go Structuring & Error Handling",
                    LearningGoal: "Produce verifiable evidence of idiomatic Go packaging, explicit error handling, and interface decoupling.",
                    LearningActions: new() { "Review Go packaging conventions and struct composition", "Analyze explicit error wrapping using fmt.Errorf and errors.Is", "Define small, focused interfaces" },
                    PracticeTask: "Implement a storage repository in Go with a file-backed and in-memory implementation satisfying a common interface, featuring comprehensive table-driven tests and explicit error wrapping.",
                    EvidenceTarget: "A Go package with table-driven tests and idiomatic error handling.",
                    CompletionCriteria: new() { "Implements idiomatic table-driven test cases", "Wraps and inspects errors using standard errors package", "Adheres to standard Go formatting and linting" }
                )
            ),
            ["java"] = (
                new SkillDetailTemplate(
                    TargetArea: "Virtual Threads & Concurrency Utilities",
                    LearningGoal: "Master Java concurrent utilities (CompletableFuture, locks, ConcurrentHashMap) and Project Loom virtual threads.",
                    LearningActions: new() { "Study virtual threads versus platform thread scheduling", "Analyze CompletableFuture composition and error handling", "Evaluate atomic references and lock-free data structures" },
                    PracticeTask: "Implement a high-concurrency data aggregator in Java using CompletableFuture and virtual threads that queries multiple simulated external APIs in parallel with timeouts and fallback handling.",
                    EvidenceTarget: "A Maven/Gradle Java project with JUnit 5 tests asserting concurrent execution and timeout resilience.",
                    CompletionCriteria: new() { "Leverages modern Java concurrency primitives effectively", "Enforces bounded timeouts and graceful fallbacks", "Passes JUnit 5 concurrency tests reliably" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Core Java OOP & Stream API",
                    LearningGoal: "Produce verifiable evidence of clean Java OOP design, Stream API data transformations, and exception handling.",
                    LearningActions: new() { "Review Java encapsulation, inheritance, and polymorphism", "Analyze Stream API map/filter/reduce operations", "Structure checked vs unchecked exception handling" },
                    PracticeTask: "Build a transaction processing service in Java that filters and aggregates financial records using Java Streams and custom exceptions, covered by JUnit 5 tests.",
                    EvidenceTarget: "A clean Java module with JUnit 5 unit tests verifying business rules and exception handling.",
                    CompletionCriteria: new() { "Applies clean OOP design principles and encapsulation", "Utilizes Java Streams for readable data transformation", "Implements robust exception handling and assertions" }
                )
            ),
            ["rust"] = (
                new SkillDetailTemplate(
                    TargetArea: "Ownership Lifetimes & Fearless Concurrency",
                    LearningGoal: "Master Rust ownership, borrow checker mechanics, explicit lifetimes, and safe multi-threaded concurrency.",
                    LearningActions: new() { "Study move semantics, Copy vs Clone, and mutable borrowing", "Analyze explicit lifetime annotations on structs and functions", "Leverage Arc, Mutex, and crossbeam channels for safe concurrency" },
                    PracticeTask: "Implement a thread-safe multi-producer multi-consumer bounded queue in Rust using Arc and Condvar/Mutex, proving absence of data races and memory leaks.",
                    EvidenceTarget: "A Cargo crate with unit and integration tests passing cargo test and clippy without warnings.",
                    CompletionCriteria: new() { "Satisfies borrow checker without unnecessary clones or unsafe code", "Demonstrates safe concurrent state synchronization", "Passes clippy lints with zero warnings" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "Idiomatic Rust & Error Handling",
                    LearningGoal: "Produce verifiable evidence of idiomatic Rust, Result/Option combinators, and pattern matching.",
                    LearningActions: new() { "Review pattern matching with match and if-let", "Analyze Result and Option monadic chaining (and_then, map)", "Define custom error enums implementing std::error::Error" },
                    PracticeTask: "Build a configuration file parser in Rust that deserializes structured settings, validates field constraints, and propagates custom strongly-typed errors using the ? operator.",
                    EvidenceTarget: "A Rust library with cargo test suite validating error propagation and valid parsing.",
                    CompletionCriteria: new() { "Uses idiomatic error propagation and custom error enums", "Applies pattern matching and combinators cleanly", "Provides comprehensive unit tests" }
                )
            ),
            ["cpp"] = (
                new SkillDetailTemplate(
                    TargetArea: "Modern C++ RAII & Move Semantics",
                    LearningGoal: "Master modern C++ (C++17/20) resource management, smart pointers, move semantics, and cache-friendly container design.",
                    LearningActions: new() { "Study RAII resource encapsulation and the Rule of Five", "Analyze std::move, rvalue references, and perfect forwarding", "Evaluate std::unique_ptr vs std::shared_ptr overhead" },
                    PracticeTask: "Implement a custom memory-mapped ring buffer in modern C++ utilizing RAII for resource management, move constructors for zero-copy transfers, and smart pointers for memory safety.",
                    EvidenceTarget: "A CMake project with unit tests and AddressSanitizer (ASan) verification proving zero memory leaks.",
                    CompletionCriteria: new() { "Adheres strictly to RAII and the Rule of Five", "Implements move semantics eliminating redundant memory copies", "Passes AddressSanitizer memory safety checks" }
                ),
                new SkillDetailTemplate(
                    TargetArea: "C++ Standard Library & Object Design",
                    LearningGoal: "Produce verifiable evidence of standard library container usage, clean class design, and const-correctness.",
                    LearningActions: new() { "Review standard library containers (std::vector, std::unordered_map)", "Analyze const-correctness and value semantics", "Structure clean class hierarchies and interfaces" },
                    PracticeTask: "Construct a student gradebook management system in modern C++ applying const-correct member functions, standard library algorithms (std::sort, std::find_if), and unit test validation.",
                    EvidenceTarget: "A C++ source module with unit tests validating algorithm correctness and memory safety.",
                    CompletionCriteria: new() { "Applies const-correctness throughout class interfaces", "Utilizes standard algorithms instead of raw loops", "Compiles with zero compiler warnings" }
                )
            )
        };

    public Task<RoadmapResponse> GenerateAsync(
        GenerateRoadmapRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.HandoffContract != null)
        {
            return GenerateFromHandoffAsync(request.HandoffContract, request.SessionId, cancellationToken);
        }

        var roleId = request.RoleId?.Trim().ToLowerInvariant() ?? string.Empty;
        var topGaps = request.TopGaps ?? new List<string>();

        // Strictly at most 3 priorities for legacy requests
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
                items.Add(new RoadmapItemDto(
                    gap,
                    priority,
                    $"Master core engineering competencies and industry best practices for {gap}.",
                    $"Complete a targeted hands-on exercise demonstrating measurable technical improvement in {gap}."
                ));
            }

            priority++;
        }

        return Task.FromResult(new RoadmapResponse(roleId, items, DateTimeOffset.UtcNow, request.SessionId));
    }

    public Task<RoadmapResponse> GenerateFromHandoffAsync(
        RoadmapHandoffContract handoff,
        string? sessionId = null,
        CancellationToken cancellationToken = default)
    {
        if (handoff == null || handoff.SkillGaps == null || handoff.SkillGaps.Count == 0)
        {
            return Task.FromResult(new RoadmapResponse(
                handoff?.RoleId ?? "backend-developer",
                new List<RoadmapItemDto>(),
                DateTimeOffset.UtcNow,
                sessionId
            ));
        }

        var roleId = handoff.RoleId?.Trim().ToLowerInvariant() ?? "backend-developer";

        // Deterministic Prioritization:
        // Group 1: Assessed Development Gaps (Beginner or Intermediate, not Insufficient Evidence)
        // Group 2: Evidence-Building Gaps (Insufficient Evidence)
        // Group 3: Next-Level Development Areas (Advanced)
        var devGaps = handoff.SkillGaps
            .Where(g => !g.CurrentLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase) &&
                        !g.CurrentLevel.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var evidenceGaps = handoff.SkillGaps
            .Where(g => g.CurrentLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var nextLevelGaps = handoff.SkillGaps
            .Where(g => g.CurrentLevel.Equals("Advanced", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Concatenate preserving order within groups
        var prioritized = new List<RoadmapSkillGapInput>();
        prioritized.AddRange(devGaps);
        prioritized.AddRange(evidenceGaps);
        prioritized.AddRange(nextLevelGaps);

        // Maximum 5 focused items; if fewer than 3 gaps exist, return exactly that number (never invent fake gaps!)
        var selectedGaps = prioritized.Take(Math.Min(5, prioritized.Count)).ToList();

        var items = new List<RoadmapItemDto>();
        int priority = 1;

        foreach (var gap in selectedGaps)
        {
            var isEvidenceGap = gap.CurrentLevel.Equals("Insufficient Evidence", StringComparison.OrdinalIgnoreCase);
            var gapType = isEvidenceGap ? "Evidence Gap" : "Development Gap";

            var item = CreateRoadmapItem(roleId, gap, priority, isEvidenceGap, gapType);
            items.Add(item);
            priority++;
        }

        // Prepare clean I9 ProjectGapContext
        var projectTargets = items.Select(i => new ProjectGapTargetSkill(
            SkillId: i.SkillId ?? i.Skill.ToLowerInvariant(),
            CurrentLevel: i.CurrentLevel ?? "Intermediate",
            TargetArea: i.TargetArea ?? i.Skill,
            PracticeTask: i.PracticeTask,
            EvidenceTarget: i.EvidenceTarget ?? "Technical implementation and documentation"
        )).ToList();

        var projectContext = new ProjectGapContext(roleId, projectTargets);

        return Task.FromResult(new RoadmapResponse(
            RoleId: roleId,
            Items: items,
            GeneratedAt: DateTimeOffset.UtcNow,
            SessionId: sessionId,
            ProjectContext: projectContext
        ));
    }

    private static RoadmapItemDto CreateRoadmapItem(
        string roleId,
        RoadmapSkillGapInput gap,
        int priority,
        bool isEvidenceGap,
        string gapType)
    {
        var skillKey = gap.SkillId?.Trim().ToLowerInvariant() ?? string.Empty;

        SkillDetailTemplate template;

        if (SkillCatalogGuidance.TryGetValue(skillKey, out var pair))
        {
            template = isEvidenceGap ? pair.Evid : pair.Dev;
        }
        else
        {
            // Generic domain fallback grounded in skill name and target area
            var target = gap.DevelopmentAreas?.FirstOrDefault() ?? (isEvidenceGap ? "Diagnostic Reasoning" : "Advanced Practices");
            template = new SkillDetailTemplate(
                TargetArea: target,
                LearningGoal: isEvidenceGap
                    ? $"Produce demonstrable evidence of diagnostic problem solving and practical capability in {gap.SkillName}."
                    : $"Deepen practical engineering capability and conceptual mastery in {gap.SkillName}, focusing on {target}.",
                LearningActions: new()
                {
                    $"Study core principles and production architecture patterns for {target}",
                    $"Review industry best practices and edge-case handling in {gap.SkillName}",
                    $"Analyze observability, performance, and failure modes in {target}"
                },
                PracticeTask: isEvidenceGap
                    ? $"Investigate a realistic scenario in {gap.SkillName} requiring diagnosis, root-cause identification, and practical remediation."
                    : $"Build a focused prototype or solve a realistic engineering problem demonstrating hands-on proficiency in {gap.SkillName} ({target}).",
                EvidenceTarget: isEvidenceGap
                    ? $"A structured technical analysis document explaining diagnostic findings and trade-offs in {gap.SkillName}."
                    : $"A runnable code implementation or Architecture Decision Record (ADR) demonstrating {target} reasoning.",
                CompletionCriteria: new()
                {
                    $"Demonstrates observable technical reasoning in {target}",
                    $"Applies correct engineering patterns without unsupported assumptions",
                    $"Produces verifiable implementation or analytical deliverables"
                }
            );
        }

        // TargetArea override if specific development areas exist in the handoff
        var targetArea = gap.DevelopmentAreas?.FirstOrDefault() ?? template.TargetArea;

        var whyThisMatters = isEvidenceGap
            ? $"The diagnostic assessment did not collect sufficient evidence to determine your current competency level in {gap.SkillName}. This activity provides a direct path to demonstrate practical capability."
            : $"Your assessment demonstrated {gap.CurrentLevel} evidence in {gap.SkillName}, while deeper {targetArea} reasoning was not yet demonstrated.";

        return new RoadmapItemDto(
            Skill: gap.SkillName,
            Priority: priority,
            LearningGoal: template.LearningGoal,
            PracticeTask: template.PracticeTask,
            SkillId: gap.SkillId,
            CurrentLevel: gap.CurrentLevel,
            GapType: gapType,
            TargetArea: targetArea,
            WhyThisMatters: whyThisMatters,
            LearningActions: template.LearningActions,
            EvidenceTarget: template.EvidenceTarget,
            CompletionCriteria: template.CompletionCriteria
        );
    }
}
