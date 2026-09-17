using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV26EvidenceVerificationTests
{
    private readonly UrlSafetyValidator _urlValidator = new();
    private readonly RepositoryEvidenceVerifier _repoVerifier;
    private readonly DeploymentEvidenceVerifier _deployVerifier;
    private readonly DataAnalystEvidenceVerifier _daVerifier;
    private readonly DeterministicProjectEvaluator _evaluator;

    public MilestoneV26EvidenceVerificationTests()
    {
        _repoVerifier = new RepositoryEvidenceVerifier(_urlValidator);
        _deployVerifier = new DeploymentEvidenceVerifier(_urlValidator);
        _daVerifier = new DataAnalystEvidenceVerifier(_urlValidator);
        _evaluator = new DeterministicProjectEvaluator(_repoVerifier, _deployVerifier, _daVerifier);
    }

    private static GapBasedProjectDto CreateSampleProject(string roleId = "backend-developer")
    {
        return new GapBasedProjectDto(
            ProjectId: "proj-be-test-01",
            RoleId: roleId,
            Title: "Resilient Order & Inventory Service",
            Scenario: "High-concurrency ordering system",
            Objective: "Deliver a reliable transactional API",
            TargetedSkills: new List<TargetedSkillDto>
            {
                new("backend.rest-apis", "intermediate", "backend.rest-apis", "REST API Deliverables"),
                new("backend.testing", "intermediate", "backend.testing", "Automated Testing Deliverables"),
                new("backend.relational-databases", "intermediate", "backend.relational-databases", "Database Schema Deliverables")
            },
            Requirements: new List<ProjectRequirementDto>
            {
                new("Build documented REST endpoints with idempotency", "backend.rest-apis", "OpenAPI REST endpoints", "backend.rest-apis"),
                new("Automated test suite exceeding 80% coverage", "backend.testing", "Automated test suite", "backend.testing"),
                new("PostgreSQL schema with composite indexing", "backend.relational-databases", "Relational database schema", "backend.relational-databases")
            },
            Deliverables: new List<string> { "OpenAPI REST endpoints", "Automated test suite", "Relational database schema" },
            EvidenceRequirements: new List<string> { "Clean repository structure", "Passing automated tests", "Documented schema" },
            EvaluationCriteria: new List<string> { "Observability", "Idempotency", "Test Coverage" },
            PortfolioOutcome: "Verified portfolio artifact"
        );
    }

    // ------------------------------------------------------------------------
    // 1. SSRF & URL Safety Validator Tests
    // ------------------------------------------------------------------------

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("http://[::1]")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://172.16.0.5/api")]
    [InlineData("http://192.168.1.1/admin")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://files.example.com")]
    [InlineData("javascript:alert(1)")]
    public void Test01_UrlSafetyValidator_RejectsRestrictedSchemesAndAddresses(string unsafeUrl)
    {
        var result = _urlValidator.ValidateUrl(unsafeUrl, allowHttpInDev: false);
        Assert.False(result.IsSafe);
        Assert.NotNull(result.FailureReason);
        Assert.Null(result.NormalizedUrl);
    }

    [Theory]
    [InlineData("https://github.com/dwyl/javascript-todo-list-tutorial")]
    [InlineData("https://api.skillproof.dev/orders")]
    [InlineData("https://demo-app.skillproof.dev/todo")]
    [InlineData("https://analytics.skillproof.dev/dashboards/revenue")]
    public void Test02_UrlSafetyValidator_AcceptsValidPublicHttpsUrls(string safeUrl)
    {
        var result = _urlValidator.ValidateUrl(safeUrl, allowHttpInDev: false);
        Assert.True(result.IsSafe);
        Assert.Null(result.FailureReason);
        Assert.NotNull(result.NormalizedUrl);
    }

    // ------------------------------------------------------------------------
    // 2. Repository Verifier Tests
    // ------------------------------------------------------------------------

    [Fact]
    public async Task Test03_RepositoryVerifier_InspectsValidRepositoryFixture()
    {
        var result = await _repoVerifier.VerifyRepositoryAsync("https://github.com/skillproof-fixtures/fe-todo-app");

        Assert.True(result.RepositoryExists);
        Assert.Equal("main", result.DefaultBranch);
        Assert.NotEmpty(result.FoundFiles);
        Assert.Contains("package.json", result.ManifestFiles);
        Assert.NotEmpty(result.TestFiles);
        Assert.Equal(VerificationState.Verified, result.Status);
        Assert.Empty(result.SecurityWarnings);
    }

    [Fact]
    public async Task Test04_RepositoryVerifier_DetectsMissingTests()
    {
        var result = await _repoVerifier.VerifyRepositoryAsync("https://github.com/skillproof-fixtures/missing-tests");

        Assert.True(result.RepositoryExists);
        Assert.Empty(result.TestFiles);
        Assert.Equal(VerificationState.PartiallyVerified, result.Status);
    }

    [Fact]
    public async Task Test05_RepositoryVerifier_FlagsSensitiveFilesWithoutContentLeakage()
    {
        var result = await _repoVerifier.VerifyRepositoryAsync("https://github.com/skillproof-fixtures/sensitive-files");

        Assert.True(result.RepositoryExists);
        Assert.NotEmpty(result.SecurityWarnings);
        Assert.Contains(result.SecurityWarnings, w => w.Contains(".env"));
        Assert.Equal(VerificationState.PartiallyVerified, result.Status);
    }

    [Fact]
    public async Task Test06_RepositoryVerifier_RejectsInvalidOrMalformedUrl()
    {
        var result = await _repoVerifier.VerifyRepositoryAsync("ftp://not-github.com/repo");

        Assert.False(result.RepositoryExists);
        Assert.Equal(VerificationState.Invalid, result.Status);
        Assert.NotNull(result.ErrorMessage);
    }

    // ------------------------------------------------------------------------
    // 3. Deployment Evidence Verifier Tests
    // ------------------------------------------------------------------------

    [Fact]
    public async Task Test07_DeploymentVerifier_VerifiesReachableWebApplication()
    {
        var result = await _deployVerifier.VerifyDeploymentAsync("https://demo-app.skillproof.dev/todo", "frontend-developer");

        Assert.True(result.IsReachable);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("HTML Web Application", result.ObservedFeatures);
        Assert.Equal(VerificationState.Verified, result.Status);
    }

    [Fact]
    public async Task Test08_DeploymentVerifier_VerifiesReachableApiEndpoint()
    {
        var result = await _deployVerifier.VerifyDeploymentAsync("https://api.skillproof.dev/orders", "backend-developer");

        Assert.True(result.IsReachable);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.Contains("JSON REST API", result.ObservedFeatures);
        Assert.Equal(VerificationState.Verified, result.Status);
    }

    [Fact]
    public async Task Test09_DeploymentVerifier_HandlesUnreachableEndpointGracefully()
    {
        var result = await _deployVerifier.VerifyDeploymentAsync("https://unreachable.example.com", "frontend-developer");

        Assert.False(result.IsReachable);
        Assert.Equal(VerificationState.Unavailable, result.Status);
    }

    // ------------------------------------------------------------------------
    // 4. Data Analyst Evidence Verifier Tests (No Website Required)
    // ------------------------------------------------------------------------

    [Fact]
    public async Task Test10_DataAnalystVerifier_VerifiesNotebookAndDatasetWithoutWebsite()
    {
        var result = await _daVerifier.VerifyDataAnalystEvidenceAsync(
            notebookUrl: "https://github.com/skillproof-fixtures/da-revenue-intelligence/blob/main/notebooks/revenue_statistical_modeling.ipynb",
            datasetUrl: "https://github.com/skillproof-fixtures/da-revenue-intelligence/raw/main/data/revenue_sample.csv",
            dashboardUrl: null,
            notes: "Cohort retention analysis and margin variances."
        );

        Assert.True(result.NotebookFound);
        Assert.NotEmpty(result.NotebookImports);
        Assert.Contains("pandas", result.NotebookImports);
        Assert.True(result.CodeCellCount > 0);
        Assert.True(result.DatasetFound);
        Assert.Equal(VerificationState.Verified, result.Status);
    }

    // ------------------------------------------------------------------------
    // 5. Evidence-Gated Evaluation & Portfolio Proof Tests
    // ------------------------------------------------------------------------

    [Fact]
    public async Task Test11_ProjectEvaluator_DemonstratedEvidenceGeneratesStrongCvProof()
    {
        var project = CreateSampleProject("backend-developer");
        var request = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://github.com/skillproof-fixtures/be-order-service",
            DeployedUrl: "https://api.skillproof.dev/orders",
            Notes: "Engineered high concurrency order service with transactional guarantees."
        );

        var evaluation = await _evaluator.EvaluateAsync(project, request);

        Assert.NotNull(evaluation);
        Assert.Equal("Demonstrated", evaluation.OverallStatus);
        Assert.NotEmpty(evaluation.RequirementResults);
        Assert.All(evaluation.RequirementResults, r => Assert.NotEmpty(r.EvidenceFound!));
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.NotEmpty(evaluation.PortfolioProof.PortfolioBullets);
        Assert.NotEmpty(evaluation.PortfolioProof.CvBullets);
        Assert.NotEmpty(evaluation.PortfolioProof.ClaimTraceability!);
    }

    [Fact]
    public async Task Test12_ProjectEvaluator_InsufficientEvidenceDoesNotGenerateStrongCvBullets()
    {
        var project = CreateSampleProject("backend-developer");
        var request = new SubmitProjectEvidenceRequest(
            RepositoryUrl: "https://unreachable.example.com/invalid-repo",
            DeployedUrl: "https://unreachable.example.com",
            Notes: ""
        );

        var evaluation = await _evaluator.EvaluateAsync(project, request);

        Assert.NotNull(evaluation);
        Assert.Equal("Insufficient Evidence", evaluation.OverallStatus);
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.Empty(evaluation.PortfolioProof.DemonstratedSkills);
        Assert.Empty(evaluation.PortfolioProof.CvBullets);
        Assert.Empty(evaluation.PortfolioProof.PortfolioBullets);
    }

    [Fact]
    public async Task Test13_ProjectEvaluator_BackwardCompatibleWithLegacyTextOnlySubmissions()
    {
        var project = CreateSampleProject("backend-developer");
        var legacyRequest = new SubmitProjectEvidenceRequest(
            ProjectSummary: "Built a robust order service with PostgreSQL transaction isolation and ACID guarantees.",
            ImplementationExplanation: "Used EF Core with optimistic concurrency, ETag headers, and connection pooling.",
            ArchitectureDecisions: "Implemented redis cache aside pattern and sliding window rate limiting returning 429 status code.",
            TestingExplanation: "Wrote unit tests and integration tests using xunit and boundary isolation mocks exceeding 85% coverage."
        );

        var evaluation = await _evaluator.EvaluateAsync(project, legacyRequest);

        Assert.NotNull(evaluation);
        Assert.Equal("Demonstrated", evaluation.OverallStatus);
        Assert.NotNull(evaluation.PortfolioProof);
        Assert.NotEmpty(evaluation.PortfolioProof.CvBullets);
    }
}
