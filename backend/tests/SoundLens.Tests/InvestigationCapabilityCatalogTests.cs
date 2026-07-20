using SoundLens.Api.Features.Agent.Common;

namespace SoundLens.Tests;

public sealed class InvestigationCapabilityCatalogTests
{
    [Fact]
    public void DistinguishesTimeDomainAndFrequencyEvidence()
    {
        var waveform = InvestigationCapabilityCatalog.All.Single(item => item.Id == "waveform");
        var spectrum = InvestigationCapabilityCatalog.All.Single(item => item.Id == "spectrum");

        Assert.Contains("time-domain", waveform.Description, StringComparison.Ordinal);
        Assert.Contains("do not use it as tonal", waveform.Description, StringComparison.Ordinal);
        Assert.Contains("tonal components", spectrum.Description, StringComparison.Ordinal);
        Assert.Contains("harmonics", spectrum.Description, StringComparison.Ordinal);
    }
}
