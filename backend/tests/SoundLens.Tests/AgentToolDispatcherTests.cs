using SoundLens.Api.Features.Agent.Tools;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;
using SoundLens.Api.Features.Waveforms.Common;
using System.Text.Json;

namespace SoundLens.Tests;

// These tests verify that AgentToolDispatcher correctly routes tool calls
// to DSP services and returns valid JSON without invoking OpenAI.
public sealed class AgentToolDispatcherTests
{
    // A minimal in-memory file store that holds the files we inject for each test.
    private sealed class StubFileStore(IReadOnlyList<ImportedFileSummary> files) : IImportedFileStore
    {
        public IReadOnlyList<ImportedFileSummary> CurrentFiles => files;
        public void Replace(IReadOnlyList<ImportedFileSummary> newFiles) { }
    }

    private static AgentToolDispatcher BuildDispatcherWithFiles(IReadOnlyList<ImportedFileSummary> files)
    {
        var store = new StubFileStore(files);
        var waveformService = new WaveformService();
        var spectrumService = new SpectrumService();
        return new AgentToolDispatcher(store, waveformService, spectrumService);
    }

    private static AgentToolDispatcher BuildDispatcherEmpty()
        => BuildDispatcherWithFiles([]);

    [Fact]
    public async Task UnknownTool_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync("nonexistent_tool", "{}", CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("Unknown tool", error.GetString());
    }

    [Fact]
    public async Task GetSignalMetrics_MissingSignalId_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.GetSignalMetrics,
            "{}",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("signalId", error.GetString());
    }

    [Fact]
    public async Task GetSignalMetrics_NoFilesImported_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.GetSignalMetrics,
            """{"signalId": "some-signal"}""",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("No files", error.GetString());
    }

    [Fact]
    public async Task GetSignalFindings_NoFilesImported_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.GetSignalFindings,
            """{"signalId": "some-signal"}""",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("No files", error.GetString());
    }

    [Fact]
    public async Task GetSpectrumSummary_NoFilesImported_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.GetSpectrumSummary,
            """{"signalId": "some-signal"}""",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("No files", error.GetString());
    }

    [Fact]
    public async Task CompareSignals_LessThanTwoIds_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.CompareSignals,
            """{"signalIds": ["only-one"]}""",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("at least 2", error.GetString());
    }

    [Fact]
    public async Task CompareSignals_MissingSignalIdsProperty_ReturnsErrorJson()
    {
        var dispatcher = BuildDispatcherEmpty();

        var result = await dispatcher.DispatchAsync(
            AgentToolDefinitions.CompareSignals,
            "{}",
            CancellationToken.None);

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out var error));
        Assert.Contains("signalIds", error.GetString());
    }
}
