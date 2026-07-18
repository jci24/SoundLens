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

    [Fact]
    public void NativeMarkdownCitation_IsRemovedAndRemapped()
    {
        const string answer = "A sourced claim. ([source](https://example.com))";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 17, answer.Length)]);

        Assert.True(WebResearchResponseParser.TryParse(result, out var normalized, out var citations));
        Assert.Equal("A sourced claim.", normalized);
        var citation = Assert.Single(citations);
        Assert.Equal(normalized.Length, citation.EndIndex);
    }

    [Fact]
    public void UnannotatedMarkdownLink_IsRejected()
    {
        const string answer = "A claim ([source](https://untrusted.example.com)).";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 0, 7)]);

        Assert.False(WebResearchResponseParser.TryParse(result, out _, out _));
    }

    [Fact]
    public void StandaloneReplacementGlyph_IsRemoved()
    {
        const string answer = "A sourced claim. \uFFFD";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 0, 15)]);

        Assert.True(WebResearchResponseParser.TryParse(result, out var normalized, out _));
        Assert.Equal("A sourced claim.", normalized);
    }

    [Fact]
    public void ReplacementGlyphInsideText_IsRejected()
    {
        const string answer = "A sour\uFFFDced claim.";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 0, answer.Length)]);

        Assert.False(WebResearchResponseParser.TryParse(result, out _, out _));
    }

    [Fact]
    public void UncitedSubstantiveParagraph_IsRejected()
    {
        const string answer = "A cited claim is available.\n\nA second claim has no source.";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 0, 13)]);

        Assert.False(WebResearchResponseParser.TryParse(result, out _, out _));
    }

    [Fact]
    public void LabelBlockDoesNotRequireItsOwnCitation()
    {
        const string answer = "Current guidance:\n\nA sourced claim is available.";
        var result = new WebResearchResult(
            answer,
            [new WebResearchCitation("Source", new Uri("https://example.com"), 19, 33)]);

        Assert.True(WebResearchResponseParser.TryParse(result, out _, out _));
    }
}
