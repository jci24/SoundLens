using System.Net;
using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class WebResearchFailurePolicyTests
{
    [Theory]
    [InlineData(0, WebResearchFailurePolicy.Network)]
    [InlineData(408, WebResearchFailurePolicy.Timeout)]
    [InlineData(429, WebResearchFailurePolicy.Throttled)]
    [InlineData(500, WebResearchFailurePolicy.Provider)]
    [InlineData(503, WebResearchFailurePolicy.Provider)]
    public void TransientStatusesAreRetryable(int statusCode, string expectedCategory)
    {
        var decision = WebResearchFailurePolicy.ClassifyStatus(statusCode);

        Assert.True(decision.ShouldRetry);
        Assert.Equal(expectedCategory, decision.Category);
    }

    [Theory]
    [InlineData(400, WebResearchFailurePolicy.InvalidRequest)]
    [InlineData(401, WebResearchFailurePolicy.Authentication)]
    [InlineData(403, WebResearchFailurePolicy.Authentication)]
    [InlineData(404, WebResearchFailurePolicy.InvalidRequest)]
    public void RequestAndAuthenticationStatusesAreNotRetryable(int statusCode, string expectedCategory)
    {
        var decision = WebResearchFailurePolicy.ClassifyStatus(statusCode);

        Assert.False(decision.ShouldRetry);
        Assert.Equal(expectedCategory, decision.Category);
    }

    [Fact]
    public void HttpStatusIsClassifiedWithoutInspectingMessageText()
    {
        var exception = new HttpRequestException(
            "This message is deliberately irrelevant.",
            null,
            HttpStatusCode.TooManyRequests);

        var decision = WebResearchFailurePolicy.Classify(exception);

        Assert.True(decision.ShouldRetry);
        Assert.Equal(WebResearchFailurePolicy.Throttled, decision.Category);
        Assert.Equal(429, decision.StatusCode);
    }

    [Fact]
    public void IncompleteSdkResponseIsRetryable()
    {
        var decision = WebResearchFailurePolicy.Classify(
            new IncompleteWebResearchResponseException(new ArgumentOutOfRangeException()));

        Assert.True(decision.ShouldRetry);
        Assert.Equal(WebResearchFailurePolicy.ResponseIncomplete, decision.Category);
        Assert.Null(decision.StatusCode);
    }
}
