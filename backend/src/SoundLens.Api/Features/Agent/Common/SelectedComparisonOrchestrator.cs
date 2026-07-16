using System.Globalization;
using OpenAI.Chat;
using SoundLens.Api.Configuration;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public sealed class SelectedComparisonResolutionException(string message, Exception innerException)
    : Exception(message, innerException);

public sealed class SelectedComparisonOrchestrator(
    IChatClientProvider chatClientProvider,
    IComparisonExplanationContextResolver comparisonContextResolver)
{
    private const string SystemPrompt = """
        You are SoundLens, an acoustic investigation copilot.
        You are explaining already selected comparison evidence, not discovering new evidence.

        RULES:
        - Use only the structured comparison context provided in this request.
        - Do not widen scope beyond the selected metric, selected aligned pair, current A/B recording pair, and optional ROI.
        - Do not invent new measurements, psychoacoustic claims, calibration state, standards claims, or root causes.
        - Do not make user-perception claims such as "sound quality", "loudness perception", "sharper", "harsher", or "listener impact" unless the user explicitly asks for perception and the provided findings directly support it.
        - If the evidence is weak, sparse, low-coverage, or shows only a very small delta, say so directly.
        - Prefer interpretation language such as "suggests", "is consistent with", "within this selected ROI", or "does not establish".
        - If the user asks "why" and the provided findings do not support a causal explanation, say that the current comparison evidence shows the difference but does not establish the cause.
        - Explain the metric in measured terms only. For crest factor, talk about the relationship between peaks and RMS level, not broader perceptual or product conclusions.
        - Keep the answer concise and engineering-focused.
        - Refer to signals using the exact resolved names provided in the context, such as "motor-a.wav · Channel 1".
        - Never repeat placeholder tokens such as "<file name>", "<displayName>", or similar template text.
        - Use only unit language that matches the selected metric. Do not say dBFS for ratio or sample-count metrics.

        RESPONSE FORMAT:
        You must respond with a JSON object that matches this exact structure:
        {
          "answer": "<grounded explanation as a plain string>",
          "citedEvidence": [
            { "toolName": "<selected_comparison_context or selected_signal_findings>", "signalId": "<signal id or empty string>", "summary": "<summary you cited>" }
          ],
          "limitations": ["<limitation 1>", "<limitation 2>"],
          "nextSteps": ["<suggested next step 1>", "<suggested next step 2>"]
        }

        citedEvidence must include the selected comparison context if you cite any comparison value.
        nextSteps should suggest 1–3 follow-up analyses or actions that stay within the current workflow.
        """;

    public async Task<AgentQueryResponse?> TryBuildResponseAsync(
        AgentQueryCommand command,
        CancellationToken ct)
    {
        if (command.ComparisonContext is null)
        {
            return null;
        }

        ResolvedComparisonExplanationContext comparisonContext;
        try
        {
            comparisonContext = await comparisonContextResolver.ResolveAsync(
                command.ComparisonContext,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }
        catch (ArgumentException exception)
        {
            throw new SelectedComparisonResolutionException(exception.Message, exception);
        }
        var isRoiScoped = command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue;
        var trustGuardResponse = SelectedComparisonTrustGuard.TryBuildResponse(
            command.Question,
            comparisonContext,
            isRoiScoped);
        if (trustGuardResponse is not null)
        {
            return trustGuardResponse;
        }

        var chatClient = chatClientProvider.GetRequiredClient();
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemPrompt),
            new UserChatMessage(BuildUserMessage(command, comparisonContext))
        };

        var completion = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions(), ct);
        var parseResult = AgentStructuredResponseParser.Parse(
            completion.Value.Content.FirstOrDefault()?.Text ?? string.Empty,
            []);
        var limitations = SelectedComparisonResponseSupport.BuildLimitations(
            comparisonContext,
            isRoiScoped);
        if (!parseResult.IsValid)
        {
            limitations = [.. limitations, AgentStructuredResponseParser.InvalidOutputLimitation];
        }

        return parseResult.Response with
        {
            CitedEvidence = SelectedComparisonResponseSupport.BuildExplanationEvidence(comparisonContext),
            Limitations = limitations,
            NextSteps = BuildNextSteps(comparisonContext)
        };
    }

    private static string BuildUserMessage(
        AgentQueryCommand command,
        ResolvedComparisonExplanationContext comparisonContext)
    {
        var observation = comparisonContext.Observation;
        var displayNameA = $"{comparisonContext.RecordingFileNameA} · {observation.DisplayNameA}";
        var displayNameB = $"{comparisonContext.RecordingFileNameB} · {observation.DisplayNameB}";

        var findingsText = comparisonContext.Findings is { Count: > 0 }
            ? string.Join(
                "\n",
                comparisonContext.Findings.Select(finding =>
                    $"- {ResolveFindingSignalDisplay(finding, comparisonContext)}: {finding.Label}{(string.IsNullOrWhiteSpace(finding.Detail) ? string.Empty : $" — {finding.Detail}")}"))
            : "None";

        var limitationText = comparisonContext.Limitations.Count > 0
            ? string.Join("\n", comparisonContext.Limitations.Select(limitation => $"- {limitation.Code}: {limitation.Detail}"))
            : "- None";

        var roiText = command.StartTimeSeconds.HasValue && command.EndTimeSeconds.HasValue
            ? $"{command.StartTimeSeconds.Value:F2}s to {command.EndTimeSeconds.Value:F2}s"
            : "Full selected duration";

        return $"""
            User question:
            {command.Question}

            Selected comparison scope:
            - Compare A: {comparisonContext.RecordingFileNameA}
            - Compare B: {comparisonContext.RecordingFileNameB}
            - Metric: {comparisonContext.MetricLabel} ({comparisonContext.MetricKey})
            - Coverage label: {comparisonContext.CoverageLabel}
            - Coverage summary: {comparisonContext.CoverageCopy}
            - Compared pairs: {comparisonContext.ComparedPairCount}
            - Missing values: {comparisonContext.MissingValueCount}
            - ROI: {roiText}

            Aggregate values:
            - Mean delta A-B: {comparisonContext.MeanDifference.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Median delta A-B: {comparisonContext.MedianDifference.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Spread: {comparisonContext.Spread.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}

            Strongest aligned pair for the selected metric:
            - Pair: {displayNameA} vs {displayNameB}
            - A value: {observation.ValueA.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - B value: {observation.ValueB.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}
            - Delta A-B: {observation.Delta.ToString("0.###", CultureInfo.InvariantCulture)} {comparisonContext.Unit}

            Visible findings for the currently inspected evidence:
            {findingsText}

            Explicit limitations:
            {limitationText}
            """;
    }

    private static string ResolveFindingSignalDisplay(
        ResolvedComparisonFinding finding,
        ResolvedComparisonExplanationContext comparisonContext)
    {
        if (string.Equals(finding.SignalId, comparisonContext.Observation.SignalIdA, StringComparison.Ordinal))
        {
            return $"{comparisonContext.RecordingFileNameA} · {comparisonContext.Observation.DisplayNameA}";
        }

        if (string.Equals(finding.SignalId, comparisonContext.Observation.SignalIdB, StringComparison.Ordinal))
        {
            return $"{comparisonContext.RecordingFileNameB} · {comparisonContext.Observation.DisplayNameB}";
        }

        return finding.SignalId;
    }

    private static IReadOnlyList<string> BuildNextSteps(
        ResolvedComparisonExplanationContext comparisonContext) =>
        comparisonContext.MetricKey switch
        {
            "crestFactorDelta" =>
            [
                "Inspect the waveform peaks for the cited pair to see whether the difference comes from transient height or average level.",
                "Check the peak and RMS cards together to see which side of the crest-factor ratio is moving more."
            ],
            "rmsAmplitudeDelta" =>
            [
                "Inspect the waveform and spectrum for the cited pair to see whether the level difference is broadband or concentrated in one band.",
                "Select a narrower region if you want to confirm whether the RMS difference is tied to one event."
            ],
            "peakAmplitudeDelta" =>
            [
                "Inspect the waveform peaks for the cited pair to locate the highest excursion.",
                "Select a narrower region if you want to verify whether the peak difference comes from one transient."
            ],
            "clippingSampleCountDelta" =>
            [
                "Inspect the waveform for the cited pair to locate where clipping occurs.",
                "Select a narrower region if you want to confirm whether clipping is confined to one event."
            ],
            _ =>
            [
                "Inspect the waveform and spectrum for the cited pair.",
                "Select another comparison metric if you need a broader explanation."
            ]
        };
}
