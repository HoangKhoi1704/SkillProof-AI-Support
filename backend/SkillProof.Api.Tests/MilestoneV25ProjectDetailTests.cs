using System.Net;
using System.Net.Http.Json;
using SkillProof.Api.Models;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV25ProjectDetailTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MilestoneV25ProjectDetailTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProjectById_ValidId_ReturnsFullSpecificationWithoutRubricLeakage()
    {
        var res = await _client.GetAsync("/api/v3/projects/proj-fe-platform-portfolio");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var project = await res.Content.ReadFromJsonAsync<CuratedProjectDto>();
        Assert.NotNull(project);
        Assert.Equal("proj-fe-platform-portfolio", project.Id);
        Assert.Equal("portfolio", project.ProjectType);
        Assert.NotEmpty(project.Deliverables);
        Assert.NotEmpty(project.EvidenceRequirements);
        Assert.NotEmpty(project.CanonicalSkillIds);
        Assert.NotEmpty(project.RoadmapTargets);
        Assert.StartsWith("https://", project.SourceUrl);
        Assert.Equal("verified", project.VerificationStatus);

        // Raw json check to ensure no internal rubric or expected signals leaked
        var rawJson = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("rubric", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expectedSignals", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("systemPrompt", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProjectById_UnknownId_Returns404NotFound()
    {
        var res = await _client.GetAsync("/api/v3/projects/non-existent-project-xyz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
