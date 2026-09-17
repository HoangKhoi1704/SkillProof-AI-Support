using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;
using SkillProof.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI
builder.Services.AddOpenApi();

// Register SQLite Catalog DbContext & Services
var connectionString = builder.Configuration.GetConnectionString("SkillProofCatalog") ?? "Data Source=skillproof_catalog.db";
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<ICatalogSeeder, CatalogSeeder>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// Register Developer AI Inspector Services
builder.Services.AddSingleton<IAiDiagnosticTraceStore, AiDiagnosticTraceStore>();
builder.Services.AddScoped<IDevAiInspectorService, DevAiInspectorService>();

// Register Adaptive Diagnostic Services
builder.Services.AddSingleton<IAdaptiveDiagnosticEngine, AdaptiveDiagnosticEngine>();
builder.Services.AddSingleton<IAdaptiveSessionStore, AdaptiveSessionStore>();
builder.Services.AddSingleton<IAdaptiveProfileBuilder, AdaptiveProfileBuilder>();
builder.Services.AddScoped<IAdaptiveDiagnosticService, AdaptiveDiagnosticService>();

// Register Diagnostic Evaluators
builder.Services.AddSingleton<DeterministicDiagnosticEvaluator>();
builder.Services.AddSingleton<IDiagnosticEvaluator>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<OpenAiDiagnosticEvaluator>>();
    var fallback = sp.GetRequiredService<DeterministicDiagnosticEvaluator>();

    var apiKey = config["OpenAI:ApiKey"];
    var model = config["OpenAI:Model"] ?? "gpt-5.4-mini";

    var isLiveAiDisabled = string.Equals(Environment.GetEnvironmentVariable("DISABLE_LIVE_AI"), "true", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(config["OpenAI:LiveEvaluationEnabled"], "false", StringComparison.OrdinalIgnoreCase);

    if (!isLiveAiDisabled && !string.IsNullOrWhiteSpace(apiKey))
    {
        return new OpenAiDiagnosticEvaluator(apiKey, model, fallback, logger);
    }

    return fallback;
});

// Register Roadmap Generators
builder.Services.AddSingleton<DeterministicRoadmapGenerator>();
builder.Services.AddSingleton<IRoadmapGenerator>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<OpenAiRoadmapGenerator>>();
    var fallback = sp.GetRequiredService<DeterministicRoadmapGenerator>();

    var apiKey = config["OpenAI:ApiKey"];
    var model = config["OpenAI:Model"] ?? "gpt-5.4-mini";

    var isLiveAiDisabled = string.Equals(Environment.GetEnvironmentVariable("DISABLE_LIVE_AI"), "true", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(config["OpenAI:LiveEvaluationEnabled"], "false", StringComparison.OrdinalIgnoreCase);

    if (!isLiveAiDisabled && !string.IsNullOrWhiteSpace(apiKey))
    {
        return new OpenAiRoadmapGenerator(apiKey, model, fallback, logger);
    }

    return fallback;
});

// Register Project Recommenders
builder.Services.AddSingleton<DeterministicProjectRecommender>();
builder.Services.AddSingleton<IProjectRecommender>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<OpenAiProjectRecommender>>();
    var fallback = sp.GetRequiredService<DeterministicProjectRecommender>();

    var apiKey = config["OpenAI:ApiKey"];
    var model = config["OpenAI:Model"] ?? "gpt-5.4-mini";

    var isLiveAiDisabled = string.Equals(Environment.GetEnvironmentVariable("DISABLE_LIVE_AI"), "true", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(config["OpenAI:LiveEvaluationEnabled"], "false", StringComparison.OrdinalIgnoreCase);

    if (!isLiveAiDisabled && !string.IsNullOrWhiteSpace(apiKey))
    {
        return new OpenAiProjectRecommender(apiKey, model, fallback, logger);
    }

    return fallback;
});

// Register Evidence Verifiers & Project Evaluators
builder.Services.AddSingleton<IUrlSafetyValidator, UrlSafetyValidator>();
builder.Services.AddSingleton<IRepositoryEvidenceVerifier, RepositoryEvidenceVerifier>();
builder.Services.AddSingleton<IDeploymentEvidenceVerifier, DeploymentEvidenceVerifier>();
builder.Services.AddSingleton<IDataAnalystEvidenceVerifier, DataAnalystEvidenceVerifier>();

builder.Services.AddSingleton<DeterministicProjectEvaluator>(sp =>
{
    var repoVerifier = sp.GetRequiredService<IRepositoryEvidenceVerifier>();
    var deployVerifier = sp.GetRequiredService<IDeploymentEvidenceVerifier>();
    var daVerifier = sp.GetRequiredService<IDataAnalystEvidenceVerifier>();
    return new DeterministicProjectEvaluator(repoVerifier, deployVerifier, daVerifier);
});

builder.Services.AddSingleton<IProjectEvaluator>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<OpenAiProjectEvaluator>>();
    var fallback = sp.GetRequiredService<DeterministicProjectEvaluator>();

    var apiKey = config["OpenAI:ApiKey"];
    var model = config["OpenAI:Model"] ?? "gpt-5.4-mini";

    var isLiveAiDisabled = string.Equals(Environment.GetEnvironmentVariable("DISABLE_LIVE_AI"), "true", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(config["OpenAI:LiveEvaluationEnabled"], "false", StringComparison.OrdinalIgnoreCase);

    if (!isLiveAiDisabled && !string.IsNullOrWhiteSpace(apiKey))
    {
        return new OpenAiProjectEvaluator(apiKey, model, fallback, logger);
    }

    return fallback;
});

builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ICanonicalRoadmapResolver, CanonicalRoadmapResolver>();
builder.Services.AddScoped<ILearningResourceService, LearningResourceService>();
builder.Services.AddScoped<ICuratedProjectMatcher, CuratedProjectMatcher>();

// Enable CORS for Next.js frontend
const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);

// Initialize & seed SQLite catalog automatically
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
    await seeder.InitializeAsync();
}

// Canonical MVP Endpoints

// 1. GET /api/roles
app.MapGet("/api/roles", (IQuestionService questionService) =>
{
    var roles = questionService.GetRoles();
    return Results.Ok(roles);
})
.WithName("GetRoles")
.WithSummary("Retrieve supported prototype career roles");

// 2. GET /api/diagnostics/questions?roleId={roleId}
app.MapGet("/api/diagnostics/questions", ([FromQuery] string? roleId, IQuestionService questionService) =>
{
    if (string.IsNullOrWhiteSpace(roleId))
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Query parameter 'roleId' is required."
        )));
    }

    if (!questionService.TryGetQuestions(roleId, out var questions) || questions == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "roleId must be one of: backend-developer, financial-analyst"
        )));
    }

    return Results.Ok(questions);
})
.WithName("GetDiagnosticQuestions")
.WithSummary("Retrieve curated career-readiness questions (rubrics excluded)");

// 2b. POST /api/diagnostics/questions/select (Dynamic SQLite Question Selection)
app.MapPost("/api/diagnostics/questions/select", async ([FromBody] DynamicQuestionSelectionRequest request, ICatalogService catalogService, CancellationToken cancellationToken) =>
{
    var (success, response, errorCode, errorMessage) = await catalogService.SelectQuestionsAsync(request, cancellationToken);
    if (!success || response == null)
    {
        if (errorCode == "NOT_FOUND")
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Resource not found.")));
        }
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode ?? "VALIDATION_ERROR", errorMessage ?? "Invalid question selection request.")));
    }

    return Results.Ok(response);
})
.WithName("SelectDiagnosticQuestions")
.WithSummary("Select dynamic assessment questions from SQLite based on user skill selection");

// 2c. POST /api/diagnostics/adaptive/sessions (Adaptive Career Readiness Diagnostic Session Start)
app.MapPost("/api/diagnostics/adaptive/sessions", async ([FromBody] AdaptiveSessionRequest request, IAdaptiveDiagnosticService adaptiveService, CancellationToken cancellationToken) =>
{
    var (success, response, errorCode, errorMessage) = await adaptiveService.StartSessionAsync(request, cancellationToken);
    if (!success || response == null)
    {
        if (errorCode == "NOT_FOUND")
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Resource not found.")));
        }
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode ?? "VALIDATION_ERROR", errorMessage ?? "Invalid adaptive session request.")));
    }

    return Results.Ok(response);
})
.WithName("StartAdaptiveSession")
.WithSummary("Start an adaptive career readiness diagnostic session");

// 2d. POST /api/diagnostics/adaptive/sessions/{sessionId}/answers (Adaptive Answer Submission)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/answers", async (string sessionId, [FromBody] AdaptiveAnswerRequest request, IAdaptiveDiagnosticService adaptiveService, CancellationToken cancellationToken) =>
{
    var (success, response, errorCode, errorMessage) = await adaptiveService.SubmitAnswerAsync(sessionId, request, cancellationToken);
    if (!success || response == null)
    {
        if (errorCode == "SESSION_NOT_FOUND")
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Session not found.")));
        }
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode ?? "VALIDATION_ERROR", errorMessage ?? "Invalid answer submission.")));
    }

    return Results.Ok(response);
})
.WithName("SubmitAdaptiveAnswer")
.WithSummary("Submit an answer in an adaptive diagnostic session and trigger branch or completion");

// 2d-2. POST /api/diagnostics/adaptive/sessions/{sessionId}/next (Advance to Next Question after Explanation)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/next", async (string sessionId, IAdaptiveDiagnosticService adaptiveService, CancellationToken cancellationToken) =>
{
    var (success, response, errorCode, errorMessage) = await adaptiveService.AdvanceNextQuestionAsync(sessionId, cancellationToken);
    if (!success || response == null)
    {
        if (errorCode == "SESSION_NOT_FOUND")
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Session not found.")));
        }
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode ?? "INVALID_STATE", errorMessage ?? "Cannot advance session.")));
    }

    return Results.Ok(response);
})
.WithName("AdvanceAdaptiveSession")
.WithSummary("Advance to the next question after reviewing post-answer explanation");

// 2e. GET /api/diagnostics/adaptive/sessions/{sessionId} (Get Adaptive Session State)
app.MapGet("/api/diagnostics/adaptive/sessions/{sessionId}", async (string sessionId, IAdaptiveDiagnosticService adaptiveService, CancellationToken cancellationToken) =>
{
    var (success, response, errorCode, errorMessage) = await adaptiveService.GetSessionAsync(sessionId, cancellationToken);
    if (!success || response == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    return Results.Ok(response);
})
.WithName("GetAdaptiveSession")
.WithSummary("Retrieve current state of an adaptive diagnostic session");

// 2f. GET /api/diagnostics/adaptive/sessions/{sessionId}/profile (Get Career Readiness Profile)
app.MapGet("/api/diagnostics/adaptive/sessions/{sessionId}/profile", async (string sessionId, IAdaptiveDiagnosticService adaptiveService, CancellationToken cancellationToken) =>
{
    var (success, profile, errorCode, errorMessage) = await adaptiveService.GetProfileAsync(sessionId, cancellationToken);
    if (!success || profile == null)
    {
        if (errorCode == "SESSION_IN_PROGRESS")
        {
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
        }
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    return Results.Ok(profile);
})
.WithName("GetAdaptiveProfile")
.WithSummary("Retrieve normalized career readiness profile and gap analysis for completed adaptive diagnostic session");

// 2g. POST /api/diagnostics/adaptive/sessions/{sessionId}/roadmap (Generate Personalized Roadmap from Profile Handoff)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/roadmap", async (string sessionId, IAdaptiveDiagnosticService adaptiveService, IAdaptiveSessionStore sessionStore, IRoadmapGenerator roadmapGenerator, CancellationToken cancellationToken) =>
{
    var (success, profile, errorCode, errorMessage) = await adaptiveService.GetProfileAsync(sessionId, cancellationToken);
    if (!success || profile == null)
    {
        if (errorCode == "SESSION_IN_PROGRESS")
        {
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
        }
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    var roadmap = await roadmapGenerator.GenerateFromHandoffAsync(profile.RoadmapInput, sessionId, cancellationToken);
    if (sessionStore.TryGetSession(sessionId, out var sState) && sState != null)
    {
        sState.Roadmap = roadmap;
        sessionStore.UpdateSession(sState);
    }
    return Results.Ok(roadmap);
})
.WithName("GenerateAdaptiveRoadmap")
.WithSummary("Generate a personalized learning roadmap from an adaptive session's trusted profile handoff");

// 2g-2. GET /api/diagnostics/adaptive/sessions/{sessionId}/canonical-roadmap (Personalized Visual Roadmap from Canonical Topology)
app.MapGet("/api/diagnostics/adaptive/sessions/{sessionId}/canonical-roadmap", async (string sessionId, ICanonicalRoadmapResolver roadmapResolver, CancellationToken cancellationToken) =>
{
    var (success, graph, errorCode, errorMessage) = await roadmapResolver.ResolveRoadmapAsync(sessionId, cancellationToken);
    if (!success || graph == null)
    {
        if (errorCode == "SESSION_IN_PROGRESS")
        {
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
        }
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    return Results.Ok(graph);
})
.WithName("GetCanonicalRoadmap")
.WithSummary("Retrieve personalized learning roadmap graph derived from Data V3 canonical topology and Assessment V2 evidence");

// 2g-3. GET /api/v3/roadmap/nodes/{nodeId}/resources (Verified Learning Resources for Canonical Node)
app.MapGet("/api/v3/roadmap/nodes/{nodeId}/resources", async (
    string nodeId,
    [FromQuery] string? roleId,
    [FromQuery] string? nodeState,
    [FromQuery] string? gapType,
    ILearningResourceService resourceService,
    CancellationToken cancellationToken) =>
{
    var response = await resourceService.GetNodeResourcesAsync(nodeId, roleId, nodeState, gapType, cancellationToken);
    return Results.Ok(response);
})
.WithName("GetNodeLearningResources")
.WithSummary("Retrieve verified canonical learning resources for a roadmap competency node");

// 2g-4. GET /api/diagnostics/adaptive/sessions/{sessionId}/projects (Curated Practice & Portfolio Projects)
app.MapGet("/api/diagnostics/adaptive/sessions/{sessionId}/projects", async (
    string sessionId,
    ICuratedProjectMatcher projectMatcher,
    CancellationToken cancellationToken) =>
{
    var (success, recommendations, errorCode, errorMessage) = await projectMatcher.MatchProjectsForSessionAsync(sessionId, cancellationToken);
    if (!success || recommendations == null)
    {
        if (errorCode == "SESSION_IN_PROGRESS")
        {
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
        }
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    return Results.Ok(recommendations);
})
.WithName("GetCuratedAdaptiveProjects")
.WithSummary("Retrieve curated practice and portfolio projects matched deterministically from trusted session gaps");

// 2g-5. POST /api/diagnostics/adaptive/sessions/{sessionId}/projects/{projectId}/select (Select Curated Portfolio Project)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/projects/{projectId}/select", async (
    string sessionId,
    string projectId,
    ICuratedProjectMatcher projectMatcher,
    CancellationToken cancellationToken) =>
{
    var gapProject = await projectMatcher.MapToGapBasedProjectAsync(projectId, sessionId, cancellationToken);
    if (gapProject == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail("PROJECT_NOT_FOUND", $"Project '{projectId}' was not found.")));
    }

    return Results.Ok(gapProject);
})
.WithName("SelectCuratedProjectForSession")
.WithSummary("Select a curated portfolio project into the adaptive session for evidence verification");

// 2g-6. GET /api/v3/projects/{projectId} (Curated Project Detail)
app.MapGet("/api/v3/projects/{projectId}", async (
    string projectId,
    ICuratedProjectMatcher projectMatcher,
    CancellationToken cancellationToken) =>
{
    var project = await projectMatcher.GetProjectByIdAsync(projectId, cancellationToken);
    if (project == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail("PROJECT_NOT_FOUND", $"Curated project '{projectId}' was not found in catalog.")));
    }

    return Results.Ok(project);
})
.WithName("GetCuratedProjectDetail")
.WithSummary("Retrieve detailed specification and deliverables for a curated project");

// 2h. POST /api/diagnostics/adaptive/sessions/{sessionId}/project/recommend (Generate Gap-Based Project from Trusted Roadmap)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/project/recommend", async (
    string sessionId,
    IAdaptiveDiagnosticService adaptiveService,
    IAdaptiveSessionStore sessionStore,
    IRoadmapGenerator roadmapGenerator,
    IProjectRecommender projectRecommender,
    ICuratedProjectMatcher curatedProjectMatcher,
    CancellationToken cancellationToken) =>
{
    var (success, profile, errorCode, errorMessage) = await adaptiveService.GetProfileAsync(sessionId, cancellationToken);
    if (!success || profile == null)
    {
        if (errorCode == "SESSION_IN_PROGRESS")
        {
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
        }
        return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
    }

    if (!sessionStore.TryGetSession(sessionId, out var sessionState) || sessionState == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail("SESSION_NOT_FOUND", "Adaptive session not found.")));
    }

    // Prefer curated portfolio project matched deterministically from catalog
    var (mSuccess, mRecs, _, _) = await curatedProjectMatcher.MatchProjectsForSessionAsync(sessionId, cancellationToken);
    if (mSuccess && mRecs != null && mRecs.PortfolioProjects.Count > 0)
    {
        var topPortfolio = mRecs.PortfolioProjects[0];
        var mappedProject = await curatedProjectMatcher.MapToGapBasedProjectAsync(topPortfolio.Id, sessionId, cancellationToken);
        if (mappedProject != null)
        {
            return Results.Ok(mappedProject);
        }
    }

    // Resolve or generate Roadmap fallback
    if (sessionState.Roadmap == null)
    {
        sessionState.Roadmap = await roadmapGenerator.GenerateFromHandoffAsync(profile.RoadmapInput, sessionId, cancellationToken);
    }

    var projectContext = sessionState.Roadmap.ProjectContext;
    if (projectContext == null || projectContext.TargetSkills.Count == 0)
    {
        var targets = sessionState.Roadmap.Items.Select(i => new ProjectGapTargetSkill(
            SkillId: i.SkillId ?? i.Skill.ToLowerInvariant(),
            CurrentLevel: i.CurrentLevel ?? "Intermediate",
            TargetArea: i.TargetArea ?? i.Skill,
            PracticeTask: i.PracticeTask,
            EvidenceTarget: i.EvidenceTarget ?? "Technical implementation and documentation"
        )).ToList();
        projectContext = new ProjectGapContext(sessionState.RoleId, targets);
    }

    var project = await projectRecommender.RecommendGapBasedAsync(projectContext, cancellationToken);
    sessionState.Project = project;
    sessionStore.UpdateSession(sessionState);

    return Results.Ok(project);
})
.WithName("RecommendAdaptiveProject")
.WithSummary("Recommend a focused gap-based real-world project engineered from trusted adaptive profile gaps");


// 2i. POST /api/diagnostics/adaptive/sessions/{sessionId}/project/submit (Submit Project Evidence & Evaluate)
app.MapPost("/api/diagnostics/adaptive/sessions/{sessionId}/project/submit", async (
    string sessionId,
    [FromBody] SubmitProjectEvidenceRequest request,
    IAdaptiveSessionStore sessionStore,
    IProjectEvaluator projectEvaluator,
    CancellationToken cancellationToken) =>
{
    if (request == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Evidence submission request cannot be empty."
        )));
    }

    if (!sessionStore.TryGetSession(sessionId, out var sessionState) || sessionState == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(
            "SESSION_NOT_FOUND",
            $"Adaptive session '{sessionId}' was not found."
        )));
    }

    if (sessionState.Project == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "PROJECT_NOT_RECOMMENDED",
            "A gap-based project must be recommended before submitting evidence."
        )));
    }

    var evaluation = await projectEvaluator.EvaluateAsync(sessionState.Project, request, cancellationToken);
    sessionState.ProjectEvaluation = evaluation;
    sessionStore.UpdateSession(sessionState);

    return Results.Ok(evaluation);
})
.WithName("SubmitAdaptiveProjectEvidence")
.WithSummary("Submit project evidence and receive qualitative evaluation and portfolio proof");

// 2j. GET /api/diagnostics/adaptive/sessions/{sessionId}/project (Retrieve Project & Evaluation)
app.MapGet("/api/diagnostics/adaptive/sessions/{sessionId}/project", (
    string sessionId,
    IAdaptiveSessionStore sessionStore) =>
{
    if (!sessionStore.TryGetSession(sessionId, out var sessionState) || sessionState == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(
            "SESSION_NOT_FOUND",
            $"Adaptive session '{sessionId}' was not found."
        )));
    }

    if (sessionState.Project == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(
            "PROJECT_NOT_FOUND",
            "No project has been recommended for this session yet."
        )));
    }

    return Results.Ok(new
    {
        Project = sessionState.Project,
        Evaluation = sessionState.ProjectEvaluation
    });
})
.WithName("GetAdaptiveProject")
.WithSummary("Retrieve recommended gap-based project and evaluation for an adaptive session");

// 3. POST /api/diagnostics/evaluate
app.MapPost("/api/diagnostics/evaluate", async ([FromBody] EvaluationRequest request, IQuestionService questionService, CancellationToken cancellationToken) =>
{
    var (success, response, errorMessage) = await questionService.EvaluateDiagnosticAsync(request, cancellationToken);
    if (!success || response == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            errorMessage ?? "Invalid diagnostic evaluation request."
        )));
    }

    return Results.Ok(response);
})
.WithName("EvaluateDiagnostic")
.WithSummary("Evaluate diagnostic answers and return qualitative skill profile with top gaps");

// 4. POST /api/roadmaps/generate
app.MapPost("/api/roadmaps/generate", async (
    [FromBody] GenerateRoadmapRequest request,
    IRoadmapGenerator roadmapGenerator,
    IAdaptiveDiagnosticService adaptiveService,
    CancellationToken cancellationToken) =>
{
    if (request == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Request body cannot be empty."
        )));
    }

    // If SessionId is provided, resolve trusted server-side handoff from Adaptive session
    if (!string.IsNullOrWhiteSpace(request.SessionId))
    {
        var (success, profile, errorCode, errorMessage) = await adaptiveService.GetProfileAsync(request.SessionId, cancellationToken);
        if (!success || profile == null)
        {
            if (errorCode == "SESSION_IN_PROGRESS")
            {
                return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Diagnostic session is still in progress.")));
            }
            return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode ?? "SESSION_NOT_FOUND", errorMessage ?? "Session not found.")));
        }

        var adaptiveResponse = await roadmapGenerator.GenerateFromHandoffAsync(profile.RoadmapInput, request.SessionId, cancellationToken);
        return Results.Ok(adaptiveResponse);
    }

    if (string.IsNullOrWhiteSpace(request.RoleId))
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "roleId is required."
        )));
    }

    var normalizedRoleId = request.RoleId.Trim().ToLowerInvariant();
    if (normalizedRoleId != "backend-developer" && normalizedRoleId != "financial-analyst")
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "roleId must be one of: backend-developer, financial-analyst"
        )));
    }

    if (request.TopGaps == null || request.TopGaps.Count == 0)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "topGaps must contain at least 1 prioritized skill gap."
        )));
    }

    var response = await roadmapGenerator.GenerateAsync(request, cancellationToken);
    return Results.Ok(response);
})
.WithName("GenerateRoadmap")
.WithSummary("Generate a personalized learning roadmap directly from diagnosed top skill gaps");

// 5. POST /api/projects/recommend
app.MapPost("/api/projects/recommend", async ([FromBody] RecommendProjectRequest request, IProjectRecommender projectRecommender, CancellationToken cancellationToken) =>
{
    if (request == null)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Request body cannot be empty."
        )));
    }

    if (string.IsNullOrWhiteSpace(request.RoleId))
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "roleId is required."
        )));
    }

    var normalizedRoleId = request.RoleId.Trim().ToLowerInvariant();
    if (normalizedRoleId != "backend-developer" && normalizedRoleId != "financial-analyst")
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "roleId must be one of: backend-developer, financial-analyst"
        )));
    }

    if (request.TopGaps == null || request.TopGaps.Count == 0)
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "topGaps must contain at least 1 prioritized skill gap."
        )));
    }

    var response = await projectRecommender.RecommendAsync(request, cancellationToken);
    return Results.Ok(response);
})
.WithName("RecommendProject")
.WithSummary("Recommend a real-world portfolio project engineered backward from diagnosed skill gaps");

// 6. GET /api/roles/{roleId}/skills (Catalog Foundation Read API)
app.MapGet("/api/roles/{roleId}/skills", async (string roleId, ICatalogService catalogService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(roleId))
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Route parameter 'roleId' is required."
        )));
    }

    var response = await catalogService.GetRoleSkillsAsync(roleId, cancellationToken);
    if (response == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(
            "NOT_FOUND",
            $"Role '{roleId}' was not found."
        )));
    }

    return Results.Ok(response);
})
.WithName("GetRoleSkills")
.WithSummary("Retrieve public UI-safe skills catalog for a career role grouped by Core, Recommended, Optional, and Languages");

// 7. GET /api/v3/roles (V3 Multi-Role Registry Read API)
app.MapGet("/api/v3/roles", async (ICatalogService catalogService, CancellationToken cancellationToken) =>
{
    var roles = await catalogService.GetV3RolesAsync(cancellationToken);
    return Results.Ok(roles);
})
.WithName("GetV3Roles")
.WithSummary("Retrieve all supported career target roles including primary demo roles and legacy roles");

// 8. GET /api/roles/{roleId}/canonical-framework (V3 Canonical Framework Read API)
app.MapGet("/api/roles/{roleId}/canonical-framework", async (string roleId, ICatalogService catalogService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(roleId))
    {
        return Results.BadRequest(new ErrorResponse(new ErrorDetail(
            "VALIDATION_ERROR",
            "Route parameter 'roleId' is required."
        )));
    }

    var framework = await catalogService.GetRoleCanonicalFrameworkAsync(roleId, cancellationToken);
    if (framework == null)
    {
        return Results.NotFound(new ErrorResponse(new ErrorDetail(
            "NOT_FOUND",
            $"Role '{roleId}' was not found in the canonical framework."
        )));
    }

    return Results.Ok(framework);
})
.WithName("GetRoleCanonicalFramework")
.WithSummary("Retrieve the V3 canonical framework, assessable competencies, and roadmap topology for a role");

// Development-Only Guard: Reject /api/dev/* outside Development environment with 404
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/dev") && !app.Environment.IsDevelopment())
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

// Development-Only Endpoints: AI Diagnostic Inspector
if (app.Environment.IsDevelopment())
{
    var devAi = app.MapGroup("/api/dev/ai");

    // GET /api/dev/ai/runtime
    devAi.MapGet("/runtime", (IDevAiInspectorService inspector) =>
    {
        return Results.Ok(inspector.GetRuntimeConfig());
    })
    .WithName("GetDevAiRuntime")
    .WithSummary("Developer-only: Retrieve safe runtime AI configuration");

    // GET /api/dev/ai/questions
    devAi.MapGet("/questions", async (
        [FromQuery] string? roleId,
        [FromQuery] string? skillId,
        [FromQuery] string? difficulty,
        IDevAiInspectorService inspector,
        CancellationToken ct) =>
    {
        var questions = await inspector.GetQuestionsAsync(roleId, skillId, difficulty, ct);
        return Results.Ok(questions);
    })
    .WithName("GetDevAiQuestions")
    .WithSummary("Developer-only: List SQLite questions with filters");

    // GET /api/dev/ai/questions/{questionId}
    devAi.MapGet("/questions/{questionId}", async (
        string questionId,
        IDevAiInspectorService inspector,
        CancellationToken ct) =>
    {
        var detail = await inspector.GetQuestionDetailAsync(questionId, ct);
        if (detail == null)
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(
                "QUESTION_NOT_FOUND",
                $"Question '{questionId}' was not found in SQLite catalog."
            )));
        }
        return Results.Ok(detail);
    })
    .WithName("GetDevAiQuestionDetail")
    .WithSummary("Developer-only: Retrieve question with server-side rubric and expected signals");

    // POST /api/dev/ai/evaluate
    devAi.MapPost("/evaluate", async (
        [FromBody] DevEvaluateRequest request,
        IDevAiInspectorService inspector,
        CancellationToken ct) =>
    {
        var (success, trace, errorCode, errorMessage) = await inspector.EvaluateAsync(request, ct);
        if (!success || trace == null)
        {
            if (errorCode == "QUESTION_NOT_FOUND")
            {
                return Results.NotFound(new ErrorResponse(new ErrorDetail(errorCode, errorMessage ?? "Question not found")));
            }
            return Results.BadRequest(new ErrorResponse(new ErrorDetail(errorCode ?? "EVALUATION_ERROR", errorMessage ?? "Evaluation failed")));
        }
        return Results.Ok(trace);
    })
    .WithName("EvaluateDevAiQuestion")
    .WithSummary("Developer-only: Execute deterministic preview or live AI evaluation and return trace");

    // GET /api/dev/ai/traces
    devAi.MapGet("/traces", (IAiDiagnosticTraceStore traceStore) =>
    {
        return Results.Ok(traceStore.GetRecentTraces());
    })
    .WithName("GetDevAiTraces")
    .WithSummary("Developer-only: List recent in-memory evaluation traces");

    // GET /api/dev/ai/traces/{traceId}
    devAi.MapGet("/traces/{traceId}", (string traceId, IAiDiagnosticTraceStore traceStore) =>
    {
        var trace = traceStore.GetTrace(traceId);
        if (trace == null)
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(
                "TRACE_NOT_FOUND",
                $"Trace '{traceId}' was not found in in-memory history."
            )));
        }
        return Results.Ok(trace);
    })
    .WithName("GetDevAiTraceDetail")
    .WithSummary("Developer-only: Retrieve full trace by ID");

    // GET /api/dev/ai/adaptive/sessions/{sessionId}
    devAi.MapGet("/adaptive/sessions/{sessionId}", async (string sessionId, IDevAiInspectorService inspector, CancellationToken ct) =>
    {
        var inspection = await inspector.InspectAdaptiveSessionAsync(sessionId, ct);
        if (inspection == null)
        {
            return Results.NotFound(new ErrorResponse(new ErrorDetail(
                "SESSION_NOT_FOUND",
                $"Adaptive session '{sessionId}' was not found."
            )));
        }
        return Results.Ok(inspection);
    })
    .WithName("GetDevAiAdaptiveSession")
    .WithSummary("Developer-only: Inspect adaptive session details delineating AI evaluation outputs from backend derivations");
}

app.Run();

// Required for WebApplicationFactory integration testing
public partial class Program { }
