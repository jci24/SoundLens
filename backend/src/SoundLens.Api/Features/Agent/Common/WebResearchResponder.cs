using System.Diagnostics;
using System.ClientModel;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class WebResearchResponder(
    IWebResearchClient webResearchClient,
    ILogger<WebResearchResponder> logger)
{
    private const int MaxAttempts = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task<AgentQueryResponse> BuildAsync(string question, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var startedAt = Stopwatch.GetTimestamp();
            try
            {
                var result = await webResearchClient.SearchAsync(question, ct);
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                if (WebResearchResponseParser.TryParse(
                        result,
                        out var answer,
                        out var citations,
                        out var failureCategory))
                {
                    logger.LogInformation(
                        "Web research succeeded on attempt {Attempt} in {ElapsedMilliseconds} ms.",
                        attempt,
                        elapsed.TotalMilliseconds);
                    return new AgentQueryResponse(
                        Answer: answer,
                        CitedEvidence: [],
                        Limitations: [],
                        NextSteps: [],
                        ToolsUsed: ["web_search"],
                        AnswerMode: AgentAnswerModes.Web)
                    {
                        ExternalCitations = citations
                    };
                }

                logger.LogWarning(
                    "Web research attempt {Attempt} produced {FailureCategory} after {ElapsedMilliseconds} ms; no retry will be attempted.",
                    attempt,
                    failureCategory,
                    elapsed.TotalMilliseconds);
                break;
            }
            catch (Exception exception) when (IsHandledFailure(exception, ct))
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                var decision = WebResearchFailurePolicy.Classify(exception);
                logger.LogWarning(
                    "Web research attempt {Attempt} failed with category {FailureCategory}, status {StatusCode}, after {ElapsedMilliseconds} ms.",
                    attempt,
                    decision.Category,
                    decision.StatusCode,
                    elapsed.TotalMilliseconds);

                if (!decision.ShouldRetry || attempt == MaxAttempts)
                {
                    break;
                }

                await Task.Delay(RetryDelay, ct);
            }
        }

        return new AgentQueryResponse(
            Answer: "Web research is temporarily unavailable, so I could not verify a current sourced answer.",
            CitedEvidence: [],
            Limitations: ["No web-sourced answer was produced."],
            NextSteps: ["Try the research question again later."],
            ToolsUsed: [],
            AnswerMode: AgentAnswerModes.Web);
    }

    private static bool IsHandledFailure(Exception exception, CancellationToken ct) =>
        exception is ClientResultException or HttpRequestException or TimeoutException or
            IncompleteWebResearchResponseException ||
        exception is TaskCanceledException && !ct.IsCancellationRequested;
}
