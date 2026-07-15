using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class AgentStructuredResponseParserTests
{
    private const string ValidResponse = """
        {
          "answer": "The selected signal has no clipping.",
          "citedEvidence": [
            { "toolName": "get_signal_metrics", "signalId": "signal-1", "summary": "hasClipping: false" }
          ],
          "limitations": [],
          "nextSteps": ["Inspect the waveform."]
        }
        """;

    [Fact]
    public void ParsesValidStructuredResponse()
    {
        var result = AgentStructuredResponseParser.Parse(ValidResponse, ["get_signal_metrics"]);

        Assert.True(result.IsValid);
        Assert.Equal("The selected signal has no clipping.", result.Response.Answer);
        Assert.Single(result.Response.CitedEvidence);
        Assert.Contains(result.Response.Limitations, limitation => limitation.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(["get_signal_metrics"], result.Response.ToolsUsed);
    }

    [Fact]
    public void ParsesOneCompleteMarkdownFence()
    {
        var result = AgentStructuredResponseParser.Parse($"```json\n{ValidResponse}\n```", []);

        Assert.True(result.IsValid);
        Assert.Equal("The selected signal has no clipping.", result.Response.Answer);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"answer\": \"truncated\"")]
    [InlineData("```json\n{ \"answer\": \"missing fence\" }")]
    [InlineData("[]")]
    [InlineData("{ \"answer\": \"Missing arrays\" }")]
    [InlineData("{ \"answer\": { \"nested\": true }, \"citedEvidence\": [], \"limitations\": [], \"nextSteps\": [] }")]
    [InlineData("{ \"answer\": \"{\\\"raw\\\":true}\", \"citedEvidence\": [], \"limitations\": [], \"nextSteps\": [] }")]
    [InlineData("{ \"answer\": \"Safe answer\", \"citedEvidence\": [], \"limitations\": [\"{\\\"raw\\\":true}\"], \"nextSteps\": [] }")]
    [InlineData("{ \"answer\": \"Unsupported tool\", \"citedEvidence\": [{ \"toolName\": \"invented_tool\", \"signalId\": \"\", \"summary\": \"\" }], \"limitations\": [], \"nextSteps\": [] }")]
    public void RejectsMalformedOrUnsafeOutputWithoutReturningIt(string rawOutput)
    {
        var result = AgentStructuredResponseParser.Parse(rawOutput, ["get_signal_metrics"]);

        Assert.False(result.IsValid);
        Assert.DoesNotContain(rawOutput, result.Response.Answer, StringComparison.Ordinal);
        Assert.Contains("could not safely interpret", result.Response.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.Response.CitedEvidence);
        Assert.Contains(result.Response.Limitations, limitation => limitation == AgentStructuredResponseParser.InvalidOutputLimitation);
        Assert.NotEmpty(result.Response.NextSteps);
        Assert.Equal(["get_signal_metrics"], result.Response.ToolsUsed);
    }
}
