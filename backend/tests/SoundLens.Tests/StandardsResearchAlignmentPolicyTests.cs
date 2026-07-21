using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class StandardsResearchAlignmentPolicyTests
{
    [Fact]
    public void OfficialCitationForEachSingleStandardIsAccepted()
    {
        const string answer = "ISO 11689:1996 defines a machinery comparison procedure.\n\nIEC 60704-1:2021 defines a household-appliance test code.";
        var citations = new[]
        {
            Citation("ISO 11689:1996", "https://www.iso.org/standard/19516.html", 0, 58),
            Citation("IEC 60704-1:2021", "https://webstore.iec.ch/en/publication/67529", 60, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO or IEC standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.True(decision.IsValid);
    }

    [Fact]
    public void MultipleStandardsWithOnlyOneMatchingCitationAreRejected()
    {
        const string answer = "ISO 11689:1996 and ISO 12001:1996 define related machinery procedures.";
        var citations = new[]
        {
            Citation("ISO 11689:1996", "https://www.iso.org/standard/19516.html", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.False(decision.IsValid);
        Assert.Equal(StandardsResearchAlignmentPolicy.UnmatchedReference, decision.FailureCategory);
    }

    [Fact]
    public void MultipleStandardsWithMatchingOfficialCitationsAreAccepted()
    {
        const string answer = "ISO 11689:1996 and ISO 12001:1996 define related machinery procedures.";
        var citations = new[]
        {
            Citation("ISO 11689:1996", "https://www.iso.org/standard/19516.html", 0, answer.Length),
            Citation("ISO 12001:1996", "https://www.iso.org/standard/21233.html", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.True(decision.IsValid);
    }

    [Fact]
    public void CitationThatDoesNotIdentifyTheStandardIsRejected()
    {
        const string answer = "ISO 11689:1996 defines a machinery comparison procedure.";
        var citations = new[]
        {
            Citation("ISO 3740:2019", "https://www.iso.org/standard/45107.html", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO standards apply?",
            answer,
            citations);

        Assert.False(decision.IsValid);
        Assert.Equal(StandardsResearchAlignmentPolicy.UnmatchedReference, decision.FailureCategory);
    }

    [Fact]
    public void SecondarySourceIsRejectedWhenPrimarySourcesWereRequested()
    {
        const string answer = "ISO 11689:1996 defines a machinery comparison procedure.";
        var citations = new[]
        {
            Citation("ISO 11689:1996", "https://example.com/iso-11689-1996", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.False(decision.IsValid);
        Assert.Equal(StandardsResearchAlignmentPolicy.MissingOfficialSource, decision.FailureCategory);
    }

    [Fact]
    public void IdentifiedSecondarySourceRemainsAllowedWithoutPrimarySourceRequest()
    {
        const string answer = "ISO 11689:1996 defines a machinery comparison procedure.";
        var citations = new[]
        {
            Citation("ISO 11689:1996", "https://example.com/iso-11689-1996", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Explain ISO 11689.",
            answer,
            citations);

        Assert.True(decision.IsValid);
    }

    [Fact]
    public void StandardsQuestionWithoutNamedStandardIsRejected()
    {
        const string answer = "An international standard defines the comparison procedure.";
        var citations = new[]
        {
            Citation("Machinery acoustics", "https://www.iso.org/standard/19516.html", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO or IEC standards apply?",
            answer,
            citations);

        Assert.False(decision.IsValid);
        Assert.Equal(StandardsResearchAlignmentPolicy.MissingStandardReference, decision.FailureCategory);
    }

    [Fact]
    public void GenericResearchIsUnaffected()
    {
        const string answer = "A current industry practice is documented.";
        var citations = new[]
        {
            Citation("Industry guidance", "https://example.com/guidance", 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "How do manufacturers compare product sound?",
            answer,
            citations);

        Assert.True(decision.IsValid);
    }

    [Theory]
    [InlineData("ISO/IEC 17025:2017", "ISO/IEC 17025:2017", "https://www.iso.org/standard/12345.html")]
    [InlineData("ISO 230-5:2000", "ISO 230-5:2000", "https://www.iso.org/standard/12345.html")]
    [InlineData("IEC 60704-2-x", "IEC 60704-2-x", "https://webstore.iec.ch/en/publication/12345")]
    public void IdentifierNormalizationSupportsJointAndPartStandards(
        string reference,
        string title,
        string url)
    {
        var answer = $"{reference} defines a relevant requirement.";
        var citations = new[]
        {
            Citation(title, url, 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO or IEC standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.True(decision.IsValid);
    }

    [Theory]
    [InlineData("ISO 11689:1996", "https://webstore.iec.ch/en/publication/12345")]
    [InlineData("IEC 60704-1:2021", "https://www.iso.org/standard/12345.html")]
    [InlineData("ISO 11689:1996", "https://ecma-international.org/publications/standards/example/")]
    public void WrongStandardsBodyCannotActAsPrimaryPublisher(string reference, string url)
    {
        var answer = $"{reference} defines a relevant requirement.";
        var citations = new[]
        {
            Citation(reference, url, 0, answer.Length)
        };

        var decision = StandardsResearchAlignmentPolicy.Validate(
            "Which ISO or IEC standards apply? Cite primary sources.",
            answer,
            citations);

        Assert.False(decision.IsValid);
        Assert.Equal(StandardsResearchAlignmentPolicy.MissingOfficialSource, decision.FailureCategory);
    }

    private static AgentExternalCitation Citation(
        string title,
        string url,
        int startIndex,
        int endIndex)
    {
        var uri = new Uri(url);
        return new AgentExternalCitation(
            title,
            url,
            startIndex,
            endIndex,
            ExternalSourceMetadataFactory.Build(uri));
    }
}
