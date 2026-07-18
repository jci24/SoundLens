using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class SelectedComparisonAnswerParserTests
{
    [Theory]
    [InlineData("{\"answer\":\"Inspect level, dynamics, frequency content, and time-varying events.\"}")]
    [InlineData("```json\n{\"answer\":\"Inspect the selected evidence first.\"}\n```")]
    [InlineData("{\"answer\":\"A valid answer\",\"unexpected\":\"ignored\"}")]
    public void AcceptsAValidatedAnswerWithoutModelOwnedEvidence(string rawText)
    {
        var result = SelectedComparisonAnswerParser.Parse(rawText);

        Assert.True(result.IsValid);
        Assert.NotEqual(SelectedComparisonAnswerParser.SafeFallbackAnswer, result.Answer);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"answer\":\"\"}")]
    [InlineData("{\"answer\":{\"nested\":true}}")]
    [InlineData("{\"answer\":\"{\\\"nested\\\":true}\"}")]
    public void RejectsMalformedOrStructuredAnswerContent(string rawText)
    {
        var result = SelectedComparisonAnswerParser.Parse(rawText);

        Assert.False(result.IsValid);
        Assert.Equal(SelectedComparisonAnswerParser.SafeFallbackAnswer, result.Answer);
    }
}
