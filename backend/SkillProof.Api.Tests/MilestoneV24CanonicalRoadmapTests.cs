using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SkillProof.Api.Data.Catalog;
using SkillProof.Api.Models;
using SkillProof.Api.Services;
using Xunit;

namespace SkillProof.Api.Tests;

public class MilestoneV24CanonicalRoadmapTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public MilestoneV24CanonicalRoadmapTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test01_AdvancedAssessedSkill_ResolvesTo_Completed()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.internet-http", "Internet & HTTP Protocols", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new() { "Strong RFC knowledge" }, "Advanced mastery", new()),
            new("frontend.html-core", "HTML5 & Web Standards", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new() { "Semantic HTML mastery" }, "Advanced mastery", new()),
            new("shared.javascript", "JavaScript Core", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new() { "Event loop mastery" }, "Advanced mastery", new())
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        var httpNode = graph.Nodes.First(n => n.CanonicalSkillId == "shared.internet-http");
        Assert.Equal(RoadmapNodeStates.Completed, httpNode.NodeState);
        Assert.Equal("NONE", httpNode.GapType);
        Assert.Contains("advanced", httpNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test02_IntermediateAssessedSkill_ResolvesTo_NeedsDevelopment_WhenGapExists()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("backend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.sql", "SQL & Relational Databases", "core", true, "Intermediate", "Demonstrated", "Demonstrated", "Emerging", "ASSESSED GAP", new() { "Join queries" }, "Demonstrated SQL queries", new() { "Transaction isolation" })
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("backend-developer", profile);

        var sqlNode = graph.Nodes.First(n => n.CanonicalSkillId == "shared.sql");
        Assert.True(sqlNode.NodeState == RoadmapNodeStates.NeedsDevelopment || sqlNode.NodeState == RoadmapNodeStates.Current);
        Assert.Equal("ASSESSED GAP", sqlNode.GapType);
        Assert.Contains("advanced reasoning", sqlNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test03_BeginnerAndInsufficientEvidence_ResolveTo_NeedsDevelopment()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("data-analyst", new List<SkillMatrixItemDto>
        {
            new("data-analyst.spreadsheets", "Spreadsheet Modeling", "core", true, "Beginner", "Emerging", "Emerging", "Insufficient Evidence", "ASSESSED GAP", new() { "Basic formulas" }, "Basic formulas", new() { "Array formulas" }),
            new("data-analyst.data-wrangling", "Data Wrangling", "core", true, "Insufficient Evidence", "Insufficient Evidence", "Insufficient Evidence", "Insufficient Evidence", "EVIDENCE GAP", new(), "Lacked detail", new() { "Tidy data principles" })
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("data-analyst", profile);

        var sheetsNode = graph.Nodes.First(n => n.CanonicalSkillId == "data-analyst.spreadsheets");
        Assert.True(sheetsNode.NodeState == RoadmapNodeStates.NeedsDevelopment || sheetsNode.NodeState == RoadmapNodeStates.Current);
        Assert.Equal("ASSESSED GAP", sheetsNode.GapType);

        var wrangNode = graph.Nodes.First(n => n.CanonicalSkillId == "data-analyst.data-wrangling");
        Assert.True(wrangNode.NodeState == RoadmapNodeStates.NeedsDevelopment || wrangNode.NodeState == RoadmapNodeStates.Current);
        Assert.Equal("EVIDENCE GAP", wrangNode.GapType);
    }

    [Fact]
    public async Task Test04_NotAssessedSkill_Remains_NotAssessed_NeverBeginner()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        // Only assessed internet-http, leave html-core unassessed
        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.internet-http", "Internet & HTTP Protocols", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new(), "Strong knowledge", new())
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        var htmlNode = graph.Nodes.First(n => n.CanonicalSkillId == "frontend.html-core");
        Assert.True(htmlNode.NodeState == RoadmapNodeStates.NotAssessed || htmlNode.NodeState == RoadmapNodeStates.Current);
        Assert.Equal("ROLE COVERAGE GAP", htmlNode.GapType);
        Assert.Equal("Not Assessed", htmlNode.AssessmentState);
        Assert.DoesNotContain("beginner", htmlNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("weak", htmlNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bad", htmlNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test05_PrerequisiteDependency_Locks_DependentNode_WhenUnresolved()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        // frontend.framework-react requires shared.javascript as prerequisite.
        // If shared.javascript is NOT Completed, React must be Locked.
        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.javascript", "JavaScript Core", "core", true, "Beginner", "Emerging", "Emerging", "Emerging", "ASSESSED GAP", new(), "Emerging JS", new() { "Event loop" })
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        var reactNode = graph.Nodes.FirstOrDefault(n => n.CanonicalSkillId == "frontend.framework-react");
        Assert.NotNull(reactNode);
        Assert.Equal(RoadmapNodeStates.Locked, reactNode.NodeState);
        Assert.Contains("prerequisite", reactNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test06_PrerequisiteDependency_Unlocks_DependentNode_WhenSatisfied()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        // When shared.javascript is Completed (Advanced), React prerequisite is satisfied!
        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.internet-http", "Internet & HTTP Protocols", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new(), "Strong knowledge", new()),
            new("frontend.html-core", "HTML5 & Web Standards", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new(), "Strong knowledge", new()),
            new("shared.javascript", "JavaScript Core", "core", true, "Advanced", "Demonstrated", "Demonstrated", "Demonstrated", "NONE", new(), "Advanced JS", new())
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        var reactNode = graph.Nodes.First(n => n.CanonicalSkillId == "frontend.framework-react");
        Assert.NotEqual(RoadmapNodeStates.Locked, reactNode.NodeState);
        Assert.True(reactNode.NodeState == RoadmapNodeStates.Current || reactNode.NodeState == RoadmapNodeStates.Available || reactNode.NodeState == RoadmapNodeStates.NotAssessed);
    }

    [Fact]
    public async Task Test07_OptionalSkill_Remains_Optional_DoesNotCreateMandatoryGap()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("data-analyst", new List<SkillMatrixItemDto>());

        var graph = await resolver.ResolveRoadmapForProfileAsync("data-analyst", profile);

        var dlNode = graph.Nodes.FirstOrDefault(n => n.CanonicalSkillId == "data-analyst.deep-learning");
        Assert.NotNull(dlNode);
        Assert.True(dlNode.IsOptional);
        Assert.Equal(RoadmapNodeStates.Optional, dlNode.NodeState);
        Assert.NotEqual("ASSESSED GAP", dlNode.GapType);
    }

    [Fact]
    public async Task Test08_ToolkitSkill_IsIsolatedFromReadinessLevels()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>());

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        var gitNode = graph.Nodes.FirstOrDefault(n => n.CanonicalSkillId == "shared.git");
        Assert.NotNull(gitNode);
        Assert.True(gitNode.IsToolkit);
        Assert.Equal("Career Toolkit", gitNode.Requirement);
        Assert.NotEqual("ASSESSED GAP", gitNode.GapType);
    }

    [Fact]
    public async Task Test09_PriorityPolicy_Selects_HighestPriorityActionableNode_As_Current()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        // Profile has an assessed gap in mandatory fundamental shared.internet-http
        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>
        {
            new("shared.internet-http", "Internet & HTTP Protocols", "core", true, "Beginner", "Emerging", "Emerging", "Insufficient Evidence", "ASSESSED GAP", new(), "Basic HTTP", new() { "HTTP/2 and DNS" })
        });

        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        Assert.NotEmpty(graph.Summary.CurrentNodeIds);
        Assert.Contains("shared.internet-http", graph.Summary.CurrentNodeIds);

        var currentNode = graph.Nodes.First(n => n.CanonicalSkillId == "shared.internet-http");
        Assert.Equal(RoadmapNodeStates.Current, currentNode.NodeState);
        Assert.Contains("priority", currentNode.WhyThisNode, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Test10_GraphSummary_HasNoNumericReadinessPercentage()
    {
        using var scope = _factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<ICanonicalRoadmapResolver>();

        var profile = CreateTestProfile("frontend-developer", new List<SkillMatrixItemDto>());
        var graph = await resolver.ResolveRoadmapForProfileAsync("frontend-developer", profile);

        // Verify summary contains counts only
        Assert.True(graph.Summary.TotalNodes > 0);
        Assert.True(graph.Summary.CompletedCount >= 0);
        Assert.True(graph.Summary.NeedsDevelopmentCount >= 0);
        Assert.True(graph.Summary.NotAssessedCount >= 0);

        // Verify no percentage strings in summary or node states
        foreach (var node in graph.Nodes)
        {
            Assert.DoesNotContain("%", node.NodeState);
            Assert.DoesNotContain("%", node.Requirement);
        }
    }

    [Fact]
    public async Task Test11_ApiEndpoint_ReturnsNotFound_ForInvalidSession()
    {
        var response = await _client.GetAsync("/api/diagnostics/adaptive/sessions/invalid-session-9999/canonical-roadmap");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("SESSION_NOT_FOUND", error.Error.Code);
    }

    [Fact]
    public async Task Test12_ApiEndpoint_ReturnsCanonicalRoadmap_ForCompletedSession()
    {
        // 1. Start session
        var startRequest = new AdaptiveSessionRequest("backend-developer", new() { "sql" });
        var startResp = await _client.PostAsJsonAsync("/api/diagnostics/adaptive/sessions", startRequest);
        startResp.EnsureSuccessStatusCode();
        var session = await startResp.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
        Assert.NotNull(session);

        // 2. Answering questions to complete session
        while (session.Status != "completed" && session.CurrentQuestion != null)
        {
            var answerResp = await _client.PostAsJsonAsync(
                $"/api/diagnostics/adaptive/sessions/{session.SessionId}/answers",
                new AdaptiveAnswerRequest(session.CurrentQuestion.Id, "Production experience with HTTP protocols, caching headers, event loop microtasks, and DOM manipulation.")
            );
            answerResp.EnsureSuccessStatusCode();
            session = await answerResp.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
            Assert.NotNull(session);

            if (session.Status != "completed")
            {
                var nextResp = await _client.PostAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/next", null);
                nextResp.EnsureSuccessStatusCode();
                session = await nextResp.Content.ReadFromJsonAsync<AdaptiveSessionResponse>();
                Assert.NotNull(session);
            }
        }

        // 3. Request canonical roadmap
        var roadmapResp = await _client.GetAsync($"/api/diagnostics/adaptive/sessions/{session.SessionId}/canonical-roadmap");
        Assert.Equal(HttpStatusCode.OK, roadmapResp.StatusCode);

        var graph = await roadmapResp.Content.ReadFromJsonAsync<PersonalizedRoadmapGraphDto>();
        Assert.NotNull(graph);
        Assert.Equal("backend-developer", graph.RoleId);
        Assert.Equal(session.SessionId, graph.SessionId);
        Assert.NotEmpty(graph.Nodes);
        Assert.NotEmpty(graph.Edges);
        Assert.NotEmpty(graph.Summary.CurrentNodeIds);

        // Security check: No raw rubric or signals exposed
        foreach (var node in graph.Nodes)
        {
            Assert.DoesNotContain("rubric", node.WhyThisNode, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("expectedSignals", node.WhyThisNode, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static CareerReadinessProfile CreateTestProfile(string roleId, List<SkillMatrixItemDto> matrixItems)
    {
        var skillProfiles = matrixItems.Select(m => new SkillProfileItemDto(
            SkillId: m.CanonicalSkillId,
            SkillName: m.SkillName,
            FinalLevel: m.OverallStatus,
            Evidence: m.EvidenceObserved,
            Reasoning: new(),
            DemonstratedStrengths: m.EvidenceObserved,
            EvidenceGaps: m.GapType == "EVIDENCE GAP" ? new() { "Insufficient evidence" } : new(),
            NextDevelopmentAreas: m.WhatToImproveNext,
            QuestionsAnswered: new() { "q1" }
        )).ToList();

        var topGaps = matrixItems
            .Where(m => m.GapType == "ASSESSED GAP" || m.GapType == "EVIDENCE GAP")
            .Select(m => m.SkillName)
            .ToList();

        var handoffGaps = matrixItems
            .Where(m => m.GapType == "ASSESSED GAP" || m.GapType == "EVIDENCE GAP")
            .Select(m => new RoadmapSkillGapInput(m.CanonicalSkillId, m.SkillName, m.OverallStatus, m.WhatToImproveNext, new()))
            .ToList();

        return new CareerReadinessProfile(
            RoleId: roleId,
            AssessmentType: "Adaptive Diagnostic V2",
            CompletedAt: DateTimeOffset.UtcNow,
            Summary: new ProfileSummaryDto(
                IntermediateCount: matrixItems.Count(m => m.OverallStatus == "Intermediate"),
                BeginnerCount: matrixItems.Count(m => m.OverallStatus == "Beginner"),
                AdvancedCount: matrixItems.Count(m => m.OverallStatus == "Advanced"),
                InsufficientEvidenceCount: matrixItems.Count(m => m.OverallStatus == "Insufficient Evidence"),
                TotalSkillsAssessed: matrixItems.Count
            ),
            Skills: skillProfiles,
            TopGaps: topGaps,
            RoadmapInput: new RoadmapHandoffContract(roleId, handoffGaps),
            SkillMatrix: matrixItems
        );
    }
}
