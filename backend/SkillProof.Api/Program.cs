using Microsoft.AspNetCore.Mvc;
using SkillProof.Api.Models;
using SkillProof.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to DI
builder.Services.AddOpenApi();

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

builder.Services.AddSingleton<IQuestionService, QuestionService>();

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
app.MapPost("/api/roadmaps/generate", async ([FromBody] GenerateRoadmapRequest request, IRoadmapGenerator roadmapGenerator, CancellationToken cancellationToken) =>
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

app.Run();

// Required for WebApplicationFactory integration testing
public partial class Program { }
