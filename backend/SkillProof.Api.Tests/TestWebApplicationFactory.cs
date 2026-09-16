using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Services;

namespace SkillProof.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Ensure automated integration test runs NEVER invoke external OpenAI APIs
            services.AddSingleton<IDiagnosticEvaluator, DeterministicDiagnosticEvaluator>();
            services.AddSingleton<IRoadmapGenerator, DeterministicRoadmapGenerator>();
            services.AddSingleton<IProjectRecommender, DeterministicProjectRecommender>();
            services.AddSingleton<IProjectEvaluator, DeterministicProjectEvaluator>();
        });
    }
}
