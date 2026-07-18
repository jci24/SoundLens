using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class GeneralKnowledgeResponseParserTests
{
    [Fact]
    public void ParsesGeneralAnswerWithoutAddingAcousticLimitations()
    {
        var response = GeneralKnowledgeResponseParser.Parse(
            """
            {
              "answer": "A Fourier transform represents a signal using frequency components.",
              "limitations": [],
              "nextSteps": ["Ask for a worked example."]
            }
            """);

        Assert.Equal(AgentAnswerModes.General, response.AnswerMode);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
        Assert.Empty(response.Limitations);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"answer\": \"truncated\"")]
    [InlineData("{ \"answer\": \"{\\\"raw\\\":true}\", \"limitations\": [], \"nextSteps\": [] }")]
    public void RejectsMalformedGeneralOutputWithoutAddingWorkspaceEvidence(string rawOutput)
    {
        var response = GeneralKnowledgeResponseParser.Parse(rawOutput);

        Assert.Equal(AgentAnswerModes.General, response.AnswerMode);
        Assert.Contains("could not safely interpret", response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(response.CitedEvidence);
        Assert.Empty(response.ToolsUsed);
        Assert.Contains(GeneralKnowledgeResponseParser.InvalidOutputLimitation, response.Limitations);
        Assert.DoesNotContain(response.Limitations, limitation => limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
    }
}
