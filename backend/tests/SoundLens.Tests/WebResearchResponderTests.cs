using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class WebResearchResponderTests
{
    [Fact]
    public async Task FirstAttemptSuccessDoesNotRetry()
    {
        var client = new SequenceWebResearchClient(ValidResult());
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Single(response.ExternalCitations);
        Assert.Equal(["web_search"], response.ToolsUsed);
    }

    [Fact]
    public async Task TransientFailureRetriesOnceAndCanRecover()
    {
        var client = new SequenceWebResearchClient(
            new HttpRequestException("temporary failure"),
            ValidResult());
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(2, client.CallCount);
        Assert.Single(response.ExternalCitations);
        Assert.Equal(["web_search"], response.ToolsUsed);
    }

    [Fact]
    public async Task IncompleteSdkResponseRetriesOnceAndCanRecover()
    {
        var client = new SequenceWebResearchClient(
            new IncompleteWebResearchResponseException(new ArgumentOutOfRangeException()),
            ValidResult());
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(2, client.CallCount);
        Assert.Single(response.ExternalCitations);
        Assert.Equal(["web_search"], response.ToolsUsed);
    }

    [Fact]
    public async Task RepeatedTransientFailureReturnsSafeUnavailableResponse()
    {
        var client = new SequenceWebResearchClient(
            new TimeoutException("first timeout"),
            new TimeoutException("second timeout"));
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(2, client.CallCount);
        Assert.Empty(response.ExternalCitations);
        Assert.Empty(response.ToolsUsed);
        Assert.Contains("temporarily unavailable", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidCitationOutputFailsClosedWithoutRetry()
    {
        var client = new SequenceWebResearchClient(new WebResearchResult(
            "Unsupported answer",
            [new WebResearchCitation("Unsafe", new Uri("file:///tmp/private.wav"), 0, 8)]));
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Empty(response.ExternalCitations);
        Assert.Contains("temporarily unavailable", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BundledStandardsClaimsFailClosedWithoutRetry()
    {
        const string answer = "ISO 11689:1996 and ISO 12001:1996 define machinery noise procedures.";
        var client = new SequenceWebResearchClient(new WebResearchResult(
            answer,
            [new WebResearchCitation(
                "ISO 11689:1996",
                new Uri("https://www.iso.org/standard/19516.html"),
                0,
                answer.Length)]));
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync(
            "Which ISO standards apply? Cite primary sources.",
            CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Empty(response.ExternalCitations);
        Assert.Contains("temporarily unavailable", response.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidRequestDoesNotRetry()
    {
        var client = new SequenceWebResearchClient(new HttpRequestException(
            "invalid request",
            null,
            HttpStatusCode.BadRequest));
        var responder = CreateResponder(client);

        var response = await responder.BuildAsync("Current guidance?", CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Empty(response.ExternalCitations);
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutRetry()
    {
        var client = new CancellingWebResearchClient();
        var responder = CreateResponder(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            responder.BuildAsync("Current guidance?", cancellation.Token));

        Assert.Equal(1, client.CallCount);
    }

    private static WebResearchResponder CreateResponder(IWebResearchClient client) =>
        new(client, NullLogger<WebResearchResponder>.Instance);

    private static WebResearchResult ValidResult()
    {
        const string answer = "Current guidance is available.";
        return new WebResearchResult(
            answer,
            [new WebResearchCitation("Primary source", new Uri("https://example.com/guidance"), 0, answer.Length)]);
    }

    private sealed class SequenceWebResearchClient(params object[] outcomes) : IWebResearchClient
    {
        public int CallCount { get; private set; }

        public Task<WebResearchResult> SearchAsync(string question, CancellationToken ct)
        {
            var outcome = outcomes[Math.Min(CallCount, outcomes.Length - 1)];
            CallCount++;
            return outcome switch
            {
                WebResearchResult result => Task.FromResult(result),
                Exception exception => Task.FromException<WebResearchResult>(exception),
                _ => throw new InvalidOperationException("Unsupported test outcome.")
            };
        }
    }

    private sealed class CancellingWebResearchClient : IWebResearchClient
    {
        public int CallCount { get; private set; }

        public Task<WebResearchResult> SearchAsync(string question, CancellationToken ct)
        {
            CallCount++;
            return Task.FromCanceled<WebResearchResult>(ct);
        }
    }
}
