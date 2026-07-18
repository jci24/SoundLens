using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class WebResearchResponseParserTests
{
    [Fact]
    public void ValidHttpCitation_IsAcceptedAndNormalized()
    {
        var result = new WebResearchResult(
            "A current standard is available.",
            [new WebResearchCitation(" Standards body ", new Uri("https://example.com/standard"), 2, 18)]);

        var parsed = WebResearchResponseParser.TryParse(result, out var answer, out var citations);

        Assert.True(parsed);
        Assert.Equal(result.Answer, answer);
        var citation = Assert.Single(citations);
        Assert.Equal("Standards body", citation.Title);
        Assert.Equal("https://example.com/standard", citation.Url);
        Assert.Equal(2, citation.StartIndex);
        Assert.Equal(18, citation.EndIndex);
    }

    [Theory]
    [InlineData("file:///tmp/private.wav")]
    [InlineData("javascript:alert(1)")]
    [InlineData("mailto:test@example.com")]
    public void UnsafeCitationScheme_IsRejected(string url)
    {
        var result = new WebResearchResult(
            "Unsafe source",
            [new WebResearchCitation("Unsafe", new Uri(url), 0, 6)]);

        Assert.False(WebResearchResponseParser.TryParse(result, out _, out _));
    }

    [Theory]
    [InlineData(-1, 4)]
    [InlineData(4, 4)]
    [InlineData(0, 20)]
    public void InvalidCitationBounds_AreRejected(int startIndex, int endIndex)
    {
        var result = new WebResearchResult(
            "Short answer",
            [new WebResearchCitation("Source", new Uri("https://example.com"), startIndex, endIndex)]);

        Assert.False(WebResearchResponseParser.TryParse(result, out _, out _));
    }

    [Fact]
    public void MissingAnswerOrCitations_IsRejected()
    {
        Assert.False(WebResearchResponseParser.TryParse(
            new WebResearchResult("", []), out _, out _));
        Assert.False(WebResearchResponseParser.TryParse(
            new WebResearchResult("No sources", []), out _, out _));
    }
}
