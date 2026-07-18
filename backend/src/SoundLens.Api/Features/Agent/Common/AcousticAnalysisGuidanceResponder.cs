using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

public static class AcousticAnalysisGuidanceResponder
{
    private static readonly string[] GuidanceTerms =
    [
        "guideline",
        "guidelines",
        "analysis workflow",
        "analysis approach",
        "how should i analyse",
        "how should i analyze",
        "how should i approach analysing",
        "how should i approach analyzing",
        "how do i analyse",
        "how do i analyze",
        "where should i start",
        "what should i inspect"
    ];

    private static readonly string[] GuidanceNouns =
    [
        "guideline",
        "workflow",
        "approach",
        "process",
        "methodology",
        "steps"
    ];

    private static readonly string[] AnalysisTargets =
    [
        "analyse",
        "analyze",
        "analysis",
        "inspect",
        "investigation",
        "recording",
        "signal",
        "audio",
        "sound",
        "file",
        "comparison"
    ];

    public static AgentQueryResponse? TryBuild(string question)
    {
        if (!IsGuidanceRequest(question))
        {
            return null;
        }

        return new AgentQueryResponse(
            Answer: """
                Use a decision-led acoustic workflow:

                1. Define the engineering question and what decision the comparison must support.
                2. Confirm the recordings are comparable: same operating condition, microphone setup, gain, environment, channel meaning, and time reference.
                3. Review duration, sample rate, channel alignment, missing channels, and any known calibration state before comparing values.
                4. Start with level and dynamics: peak, RMS, crest factor, and clipping. Treat these as different measurements rather than one overall quality score.
                5. Inspect the waveform for transients, repeated events, timing changes, and useful regions of interest.
                6. Inspect the spectrum for broadband balance, tonal components, harmonics, and frequency bands that differ.
                7. Audition the original recordings and relevant channels at a controlled playback level, using the plots to guide listening rather than replacing it.
                8. Repeat important comparisons on matched regions and check whether the result is consistent across channels or repetitions.
                9. Record exclusions, coverage gaps, calibration limits, and alternative explanations before drawing a conclusion.
                10. Change one test variable at a time and repeat the measurement before attributing a cause.
                """,
            CitedEvidence: [],
            Limitations:
            [
                "These are investigation guidelines; no new measurement was performed for this answer.",
                "Digital amplitude values must not be interpreted as physical SPL without validated acoustic calibration."
            ],
            NextSteps:
            [
                "Define the decision you need these recordings to support.",
                "Verify that recording conditions and channel meanings are comparable before selecting metrics or an ROI."
            ],
            ToolsUsed: [],
            AnswerMode: AgentAnswerModes.General);
    }

    public static bool IsGuidanceRequest(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();
        return GuidanceTerms.Any(term => normalized.Contains(term, StringComparison.Ordinal)) ||
            GuidanceNouns.Any(term => normalized.Contains(term, StringComparison.Ordinal)) &&
            AnalysisTargets.Any(target => normalized.Contains(target, StringComparison.Ordinal));
    }
}
