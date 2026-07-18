using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class InvestigationGuidanceResponseParserTests
{
    [Fact]
    public void ParsesValidatedGuidanceWithoutEvidenceClaims()
    {
        var response = InvestigationGuidanceResponseParser.Parse(
            """
            {
              "answer": "First clarify whether the decision concerns level consistency or tonal character.",
              "limitations": ["No calibration state is available."],
              "recommendedCapabilityIds": ["waveform"]
            }
            """,
            InvestigationCapabilityCatalog.ResolveAvailable(hasRecordings: true, hasComparisonPair: true));

        Assert.Equal(AgentAnswerModes.Guidance, response.AnswerMode);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
        Assert.Single(response.Limitations);
        Assert.Single(response.NextSteps);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"answer\":\"ok\",\"limitations\":[],\"recommendedCapabilityIds\":\"invalid\"}")]
    [InlineData("{\"answer\":\"{\\\"measurement\\\":42}\",\"limitations\":[],\"recommendedCapabilityIds\":[]}")]
    [InlineData("{\"answer\":\"Use the spectrogram.\",\"limitations\":[],\"recommendedCapabilityIds\":[\"spectrogram\"]}")]
    [InlineData("{\"answer\":\"Export a report.\",\"limitations\":[],\"recommendedCapabilityIds\":[\"report_export\"]}")]
    public void RejectsMalformedOrStructuredPayloads(string rawText)
    {
        var response = InvestigationGuidanceResponseParser.Parse(
            rawText,
            InvestigationCapabilityCatalog.ResolveAvailable(hasRecordings: true, hasComparisonPair: false));

        Assert.Equal(AgentAnswerModes.Guidance, response.AnswerMode);
        Assert.Contains("could not safely prepare", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(InvestigationGuidanceResponseParser.InvalidOutputLimitation, response.Limitations);
    }
}
