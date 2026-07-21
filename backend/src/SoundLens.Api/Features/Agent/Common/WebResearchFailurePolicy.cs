using System.ClientModel;

namespace SoundLens.Api.Features.Agent.Common;

public sealed record WebResearchFailureDecision(
    string Category,
    bool ShouldRetry,
    int? StatusCode = null);

public static class WebResearchFailurePolicy
{
    public const string Authentication = "authentication";
    public const string InvalidRequest = "invalid_request";
    public const string Network = "network";
    public const string Provider = "provider";
    public const string ResponseIncomplete = "response_incomplete";
    public const string Throttled = "throttled";
    public const string Timeout = "timeout";

    public static WebResearchFailureDecision Classify(Exception exception) => exception switch
    {
        TimeoutException or TaskCanceledException => new(Timeout, true),
        IncompleteWebResearchResponseException => new(ResponseIncomplete, true),
        HttpRequestException httpException => ClassifyStatus(
            httpException.StatusCode is null ? 0 : (int)httpException.StatusCode.Value),
        ClientResultException clientException => ClassifyStatus(clientException.Status),
        _ => new(Provider, false)
    };

    public static WebResearchFailureDecision ClassifyStatus(int statusCode)
    {
        if (statusCode is 0)
        {
            return new WebResearchFailureDecision(Network, true);
        }

        if (statusCode is 408)
        {
            return new WebResearchFailureDecision(Timeout, true, statusCode);
        }

        if (statusCode is 429)
        {
            return new WebResearchFailureDecision(Throttled, true, statusCode);
        }

        if (statusCode is >= 500 and <= 599)
        {
            return new WebResearchFailureDecision(Provider, true, statusCode);
        }

        if (statusCode is 401 or 403)
        {
            return new WebResearchFailureDecision(Authentication, false, statusCode);
        }

        return new WebResearchFailureDecision(InvalidRequest, false, statusCode);
    }
}
