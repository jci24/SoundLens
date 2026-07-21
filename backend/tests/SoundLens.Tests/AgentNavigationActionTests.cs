using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class AgentNavigationActionTests
{
    [Theory]
    [InlineData("How do I import recordings?", AgentRouteNames.Home, "navigate_import")]
    [InlineData("Where can I configure the comparison?", AgentRouteNames.Home, "navigate_configure")]
    [InlineData("Show me the analysis methods.", AgentRouteNames.Configure, "navigate_analysis")]
    [InlineData("Open the waveform evidence.", AgentRouteNames.Analysis, "navigate_evidence")]
    public void Resolve_ReturnsOneAllowlistedActionForExplicitNavigationIntent(
        string question,
        string currentRoute,
        string expectedActionId)
    {
        var action = Assert.Single(AgentNavigationSuggestionPolicy.Resolve(question, currentRoute));

        Assert.Equal(expectedActionId, action.ActionId);
        Assert.Equal(AgentActionKinds.Navigate, action.Kind);
        Assert.True(AgentRouteNames.IsSupported(action.TargetRoute));
    }

    [Theory]
    [InlineData("What is a waveform?")]
    [InlineData("Compare these recordings.")]
    [InlineData("The spectrum shows a tonal peak.")]
    public void Resolve_DoesNotAddActionsWithoutExplicitNavigationIntent(string question)
    {
        Assert.Empty(AgentNavigationSuggestionPolicy.Resolve(question, AgentRouteNames.Home));
    }

    [Fact]
    public void Resolve_DoesNotSuggestTheCurrentRoute()
    {
        Assert.Empty(AgentNavigationSuggestionPolicy.Resolve(
            "Open the evidence workspace.",
            AgentRouteNames.Evidence));
    }

    [Fact]
    public void Decorator_AddsOnlyTheBoundedActionContractToTheCompletedResponse()
    {
        var response = new AgentQueryResponse("Use the import page.", [], [], [], [], AgentAnswerModes.General);
        var command = new AgentQueryCommand(
            "How do I import recordings?",
            null,
            null,
            null,
            RouteContext: new AgentRouteContext(AgentRouteNames.Home));

        var decorated = AgentResponseActionDecorator.AddSuggestedActions(response, command);
        var action = Assert.Single(decorated.SuggestedActions);

        Assert.Equal("navigate_import", action.ActionId);
        Assert.Equal(AgentRouteNames.Import, action.TargetRoute);
        Assert.Equal("Use the import page.", decorated.Answer);
        Assert.Empty(decorated.CitedEvidence);
    }

    [Fact]
    public async Task Approval_RejectsUnknownAndStaleActionsAndReturnsBackendOwnedReceipt()
    {
        var store = new InMemoryImportedFileStore();
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IImportedFileStore>();
                services.AddSingleton<IImportedFileStore>(store);
            });
        });
        using var client = factory.CreateClient();

        var unknown = await client.PostAsJsonAsync("/api/agent/actions/navigation", new
        {
            actionId = "navigate_https_example_com",
            currentRoute = AgentRouteNames.Home,
            previousActivitySequence = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, unknown.StatusCode);

        var stale = await client.PostAsJsonAsync("/api/agent/actions/navigation", new
        {
            actionId = "navigate_evidence",
            currentRoute = AgentRouteNames.Home,
            previousActivitySequence = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var replayed = await client.PostAsJsonAsync("/api/agent/actions/navigation", new
        {
            actionId = "navigate_import",
            currentRoute = AgentRouteNames.Import,
            previousActivitySequence = 0
        });
        Assert.Equal(HttpStatusCode.Conflict, replayed.StatusCode);

        store.Replace([new ImportedFileSummary("example.wav", 4, "/tmp/example.wav", "audio/wav")]);
        var approved = await client.PostAsJsonAsync("/api/agent/actions/navigation", new
        {
            actionId = "navigate_evidence",
            currentRoute = AgentRouteNames.Home,
            previousActivitySequence = 4
        });
        approved.EnsureSuccessStatusCode();
        var payload = await approved.Content.ReadFromJsonAsync<AgentNavigationActionResponse>();

        Assert.NotNull(payload);
        Assert.Equal(AgentRouteNames.Evidence, payload!.TargetRoute);
        Assert.Equal(5, payload.Activity.Sequence);
        Assert.Equal(AgentActivityKinds.Action, payload.Activity.Kind);
        Assert.Equal(AgentActivityStatuses.Completed, payload.Activity.Status);
        Assert.DoesNotContain("example.wav", payload.Activity.Summary, StringComparison.Ordinal);
    }
}
