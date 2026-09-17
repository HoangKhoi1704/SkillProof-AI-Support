using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;

namespace SkillProof.Api.Tests;

public class MilestoneV21BCanonicalDataV3Tests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public MilestoneV21BCanonicalDataV3Tests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Roles_ThreePrimaryDemoRolesExist_PlusLegacyRole()
    {
        var response = await _client.GetAsync("/api/v3/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleSummaryDto>>();
        Assert.NotNull(roles);
        Assert.Equal(4, roles.Count);

        var primaryRoles = roles.Where(r => r.IsPrimaryDemoRole).ToList();
        Assert.Equal(3, primaryRoles.Count);

        var primaryRoleIds = primaryRoles.Select(r => r.Id).ToHashSet();
        Assert.Contains("frontend-developer", primaryRoleIds);
        Assert.Contains("backend-developer", primaryRoleIds);
        Assert.Contains("data-analyst", primaryRoleIds);

        var legacyRole = roles.FirstOrDefault(r => r.Id == "financial-analyst");
        Assert.NotNull(legacyRole);
        Assert.False(legacyRole.IsPrimaryDemoRole);
    }

    [Fact]
    public async Task CanonicalSkills_IdsAreUnique_AndMatchApproved59Count()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var skills = await db.CanonicalSkills.ToListAsync();
        Assert.Equal(59, skills.Count);

        var distinctIds = skills.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(59, distinctIds.Count);

        // Verify prefixes
        var sharedCount = skills.Count(s => s.Id.StartsWith("shared."));
        var feCount = skills.Count(s => s.Id.StartsWith("frontend."));
        var beCount = skills.Count(s => s.Id.StartsWith("backend."));
        var daCount = skills.Count(s => s.Id.StartsWith("data-analyst."));
        var extCount = skills.Count(s => s.Id.StartsWith("assessment-ext."));
        var toolkitCount = skills.Count(s => s.Id.StartsWith("toolkit."));

        Assert.Equal(6, sharedCount);
        Assert.Equal(14, feCount);
        Assert.Equal(20, beCount);
        Assert.Equal(12, daCount);
        Assert.Equal(3, extCount);
        Assert.Equal(4, toolkitCount);
    }

    [Fact]
    public async Task AssessmentPolicyExtensions_HaveExplicitLabeling_NoFakeRoadmapIds()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var extensions = await db.CanonicalSkills
            .Where(s => s.Classification == "assessment-policy-extension")
            .ToListAsync();

        Assert.Equal(3, extensions.Count);

        var expectedExtIds = new[]
        {
            "assessment-ext.programming-fundamentals",
            "assessment-ext.oop-solid",
            "assessment-ext.data-storytelling"
        };

        foreach (var extId in expectedExtIds)
        {
            var ext = extensions.FirstOrDefault(e => e.Id == extId);
            Assert.NotNull(ext);
            Assert.Equal("assessment-policy", ext.SourceKind);
            Assert.Null(ext.RoadmapNodeId);
            Assert.Null(ext.RoadmapSource);
        }
    }

    [Fact]
    public async Task FrontendFramework_MetaFrameworks_AreNonMandatoryElectives()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var node = await db.RoleRoadmapNodes
            .FirstOrDefaultAsync(rn => rn.RoleId == "frontend-developer" && rn.CanonicalSkillId == "frontend.meta-frameworks");

        Assert.NotNull(node);
        Assert.False(node.MandatoryFundamental, "Next.js/Remix/Astro meta-frameworks must NOT be mandatory fundamentals.");
        Assert.False(node.IsOptional);
    }

    [Fact]
    public async Task DataAnalyst_DeepLearning_IsOptionalAdvanced_NotCoreReadiness()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var node = await db.RoleRoadmapNodes
            .FirstOrDefaultAsync(rn => rn.RoleId == "data-analyst" && rn.CanonicalSkillId == "data-analyst.deep-learning");

        Assert.NotNull(node);
        Assert.True(node.IsOptional, "Data Analyst Deep Learning must be explicitly Optional / Advanced.");
        Assert.False(node.MandatoryFundamental, "Data Analyst Deep Learning must NOT be a mandatory fundamental.");
        Assert.False(node.AssessmentEligible, "Data Analyst Deep Learning is excluded from core interview readiness assessment.");
    }

    [Fact]
    public async Task MandatoryFundamentals_AreExplicitlyMarkedAcrossRoles()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Frontend mandatory
        var feMandatory = await db.RoleRoadmapNodes
            .Where(rn => rn.RoleId == "frontend-developer" && rn.MandatoryFundamental)
            .Select(rn => rn.CanonicalSkillId)
            .ToListAsync();

        Assert.Contains("shared.internet-http", feMandatory);
        Assert.Contains("frontend.html-core", feMandatory);
        Assert.Contains("shared.javascript", feMandatory);
        Assert.Contains("frontend.web-security", feMandatory);
        Assert.Equal(4, feMandatory.Count);

        // Backend mandatory
        var beMandatory = await db.RoleRoadmapNodes
            .Where(rn => rn.RoleId == "backend-developer" && rn.MandatoryFundamental)
            .Select(rn => rn.CanonicalSkillId)
            .ToListAsync();

        Assert.Contains("backend.language-selection", beMandatory);
        Assert.Contains("assessment-ext.programming-fundamentals", beMandatory);
        Assert.Contains("backend.rest-apis", beMandatory);
        Assert.Contains("backend.relational-databases", beMandatory);
        Assert.Contains("backend.testing", beMandatory);
        Assert.Contains("backend.authentication-security", beMandatory);
        Assert.Contains("backend.system-design", beMandatory);
        Assert.Equal(7, beMandatory.Count);

        // Data Analyst mandatory
        var daMandatory = await db.RoleRoadmapNodes
            .Where(rn => rn.RoleId == "data-analyst" && rn.MandatoryFundamental)
            .Select(rn => rn.CanonicalSkillId)
            .ToListAsync();

        Assert.Contains("data-analyst.spreadsheets", daMandatory);
        Assert.Contains("shared.sql", daMandatory);
        Assert.Contains("data-analyst.python-or-r", daMandatory);
        Assert.Contains("data-analyst.data-wrangling", daMandatory);
        Assert.Contains("data-analyst.statistics", daMandatory);
        Assert.Contains("data-analyst.eda", daMandatory);
        Assert.Equal(6, daMandatory.Count);
    }

    [Fact]
    public async Task BackendLegacyMapping_All22SkillsMapToValidV3CanonicalSkills()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var mappings = await db.LegacySkillMappings.ToListAsync();
        Assert.Equal(22, mappings.Count);

        var canonicalIds = (await db.CanonicalSkills.Select(cs => cs.Id).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            Assert.Contains(mapping.CanonicalSkillId, canonicalIds);
            Assert.Equal("backend-developer", mapping.RoleId);
        }

        // Check key mappings
        var restMapping = mappings.First(m => m.LegacySkillId == "rest-api");
        Assert.Equal("backend.rest-apis", restMapping.CanonicalSkillId);

        var sqlMapping = mappings.First(m => m.LegacySkillId == "sql");
        Assert.Equal("shared.sql", sqlMapping.CanonicalSkillId);

        var progMapping = mappings.First(m => m.LegacySkillId == "programming-fundamentals");
        Assert.Equal("assessment-ext.programming-fundamentals", progMapping.CanonicalSkillId);
    }

    [Fact]
    public async Task BackendQuestionMapping_All48QuestionsResolveToCanonicalSkills()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var questions = await db.Questions.ToListAsync();
        Assert.Equal(48, questions.Count);

        var mappings = await db.LegacySkillMappings.ToDictionaryAsync(m => m.LegacySkillId, m => m.CanonicalSkillId);
        var canonicalIds = (await db.CanonicalSkills.Select(cs => cs.Id).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var q in questions)
        {
            Assert.True(mappings.TryGetValue(q.SkillId, out var canonicalSkillId),
                $"Question '{q.Id}' has legacy skill '{q.SkillId}' which must exist in LegacySkillMappings.");
            Assert.Contains(canonicalSkillId, canonicalIds);
        }
    }

    [Fact]
    public async Task RoleFrameworkEndpoint_FrontendDeveloper_Returns18NodesAndTopology()
    {
        var response = await _client.GetAsync("/api/roles/frontend-developer/canonical-framework");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var framework = await response.Content.ReadFromJsonAsync<RoleCanonicalFrameworkResponse>();
        Assert.NotNull(framework);
        Assert.Equal("frontend-developer", framework.RoleId);
        Assert.Equal(18, framework.Nodes.Count);
        Assert.True(framework.Relationships.Count > 0, "Frontend framework must include pedagogical relationships.");

        // Frontend question coverage: 4 mandatory fundamentals have coverage in V2.3, others remain uncovered
        var mandatoryWithCoverage = new HashSet<string> { "shared.internet-http", "frontend.html-core", "shared.javascript", "frontend.web-security" };
        Assert.All(framework.Nodes, node =>
        {
            if (mandatoryWithCoverage.Contains(node.CanonicalSkillId))
            {
                Assert.True(node.HasQuestionCoverage, $"Node '{node.CanonicalSkillId}' must report question coverage.");
            }
            else
            {
                Assert.False(node.HasQuestionCoverage, $"Node '{node.CanonicalSkillId}' must honestly report no question coverage.");
            }
        });
    }

    [Fact]
    public async Task RoleFrameworkEndpoint_BackendDeveloper_Returns26NodesWithQuestionCoverage()
    {
        var response = await _client.GetAsync("/api/roles/backend-developer/canonical-framework");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var framework = await response.Content.ReadFromJsonAsync<RoleCanonicalFrameworkResponse>();
        Assert.NotNull(framework);
        Assert.Equal("backend-developer", framework.RoleId);
        Assert.Equal(26, framework.Nodes.Count);

        // Core assessable competencies have question coverage
        var restNode = framework.Nodes.FirstOrDefault(n => n.CanonicalSkillId == "backend.rest-apis");
        Assert.NotNull(restNode);
        Assert.True(restNode.HasQuestionCoverage);
        Assert.True(restNode.AssessmentEligible);
        Assert.True(restNode.MandatoryFundamental);
    }

    [Fact]
    public async Task RoleFrameworkEndpoint_DataAnalyst_Returns14Nodes()
    {
        var response = await _client.GetAsync("/api/roles/data-analyst/canonical-framework");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var framework = await response.Content.ReadFromJsonAsync<RoleCanonicalFrameworkResponse>();
        Assert.NotNull(framework);
        Assert.Equal("data-analyst", framework.RoleId);
        Assert.Equal(14, framework.Nodes.Count);
    }

    [Fact]
    public async Task RoleFrameworkEndpoint_UnknownRole_Returns404NotFound()
    {
        var response = await _client.GetAsync("/api/roles/non-existent-role/canonical-framework");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Seeder_ReentrantAndIdempotent_PreservesCountsExactly()
    {
        using var scope = _factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ICatalogSeeder>();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Re-seed multiple times
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(4, await db.Roles.CountAsync());
        Assert.Equal(59, await db.CanonicalSkills.CountAsync());
        Assert.Equal(58, await db.RoleRoadmapNodes.CountAsync());
        Assert.Equal(19, await db.RoadmapRelationships.CountAsync());
        Assert.Equal(22, await db.LegacySkillMappings.CountAsync());
        Assert.Equal(48, await db.Questions.CountAsync());
    }
}
