using System.Net;
using System.Net.Http.Json;
using SkillProof.Api.Models;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV25ResourcesTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MilestoneV25ResourcesTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetNodeResources_ValidCanonicalNode_ReturnsCuratedOfficialResources()
    {
        // Act: GET /api/v3/roadmap/nodes/frontend.html-core/resources
        var res = await _client.GetAsync("/api/v3/roadmap/nodes/frontend.html-core/resources?roleId=frontend-developer");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(body);
        Assert.Equal("frontend.html-core", body.CanonicalSkillId);
        Assert.NotEmpty(body.Resources);

        var first = body.Resources.First();
        Assert.Equal("res-fe-html-mdn-01", first.Id);
        Assert.Contains("MDN Web Docs", first.SourceName);
        Assert.StartsWith("https://", first.SourceUrl);
        Assert.True(first.IsOfficial);
        Assert.Equal("verified", first.VerificationStatus);
        Assert.False(string.IsNullOrWhiteSpace(first.RelevanceReason));
    }

    [Fact]
    public async Task GetNodeResources_LevelSelection_AdaptsToNodeStateDeterministically()
    {
        // Act 1: NeedsDevelopment state
        var resDev = await _client.GetAsync("/api/v3/roadmap/nodes/shared.javascript/resources?nodeState=NeedsDevelopment&gapType=ASSESSED_CORE_GAP");
        Assert.Equal(HttpStatusCode.OK, resDev.StatusCode);
        var bodyDev = await resDev.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(bodyDev);
        Assert.NotEmpty(bodyDev.Resources);
        Assert.Contains("developmental gaps", bodyDev.Resources.First().RelevanceReason ?? "");

        // Act 2: Completed state
        var resComp = await _client.GetAsync("/api/v3/roadmap/nodes/shared.javascript/resources?nodeState=Completed");
        Assert.Equal(HttpStatusCode.OK, resComp.StatusCode);
        var bodyComp = await resComp.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(bodyComp);
        Assert.NotEmpty(bodyComp.Resources);
        Assert.Contains("completed competency", bodyComp.Resources.First().RelevanceReason ?? "");
    }

    [Fact]
    public async Task GetNodeResources_UnsupportedNode_ReturnsEmptyList_NoHallucination()
    {
        // Act: Request for a node that has no curated resource
        var res = await _client.GetAsync("/api/v3/roadmap/nodes/toolkit.ide-mastery/resources");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(body);
        Assert.Empty(body.Resources);
    }

    [Fact]
    public async Task GetNodeResources_EnforcesZeroInventedUrls_AndNoYouTube()
    {
        // Query resources across multiple skills
        var skillsToTest = new[] { "frontend.html-core", "shared.javascript", "backend.rest-apis", "data-analyst.spreadsheets" };

        foreach (var skillId in skillsToTest)
        {
            var res = await _client.GetAsync($"/api/v3/roadmap/nodes/{skillId}/resources");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);

            var body = await res.Content.ReadFromJsonAsync<NodeResourcesResponse>();
            Assert.NotNull(body);

            foreach (var r in body.Resources)
            {
                Assert.StartsWith("https://", r.SourceUrl);
                Assert.DoesNotContain("youtube.com", r.SourceUrl);
                Assert.DoesNotContain("youtu.be", r.SourceUrl);
                Assert.False(string.IsNullOrWhiteSpace(r.VerifiedAt));
                Assert.Equal("verified", r.VerificationStatus);
            }
        }
    }

    [Fact]
    public async Task GetNodeResources_FiltersRoleApplicabilityProperly()
    {
        // Query Excel for data-analyst -> should return resource
        var resDa = await _client.GetAsync("/api/v3/roadmap/nodes/data-analyst.spreadsheets/resources?roleId=data-analyst");
        Assert.Equal(HttpStatusCode.OK, resDa.StatusCode);
        var bodyDa = await resDa.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(bodyDa);
        Assert.NotEmpty(bodyDa.Resources);

        // Query Excel for frontend-developer -> should return empty list
        var resFe = await _client.GetAsync("/api/v3/roadmap/nodes/data-analyst.spreadsheets/resources?roleId=frontend-developer");
        Assert.Equal(HttpStatusCode.OK, resFe.StatusCode);
        var bodyFe = await resFe.Content.ReadFromJsonAsync<NodeResourcesResponse>();
        Assert.NotNull(bodyFe);
        Assert.Empty(bodyFe.Resources);
    }
}
