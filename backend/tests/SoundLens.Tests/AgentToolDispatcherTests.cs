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

    private static byte[] CreateMono16BitWav(int sampleRate, short[] samples)
    {
        var bytesPerSample = 2;
        var channelCount = 1;
        var blockAlign = channelCount * bytesPerSample;
        var byteRate = sampleRate * blockAlign;
        var dataSize = samples.Length * blockAlign;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channelCount);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)(bytesPerSample * 8));
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static byte[] CreateStereo16BitWav(int sampleRate, short[] leftSamples, short[] rightSamples)
    {
        if (leftSamples.Length != rightSamples.Length)
        {
            throw new ArgumentException("Left and right channel sample counts must match.");
        }

        var bytesPerSample = 2;
        var channelCount = 2;
        var blockAlign = channelCount * bytesPerSample;
        var byteRate = sampleRate * blockAlign;
        var dataSize = leftSamples.Length * blockAlign;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channelCount);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)(bytesPerSample * 8));
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (var index = 0; index < leftSamples.Length; index++)
        {
            writer.Write(leftSamples[index]);
            writer.Write(rightSamples[index]);
        }

        writer.Flush();
        return stream.ToArray();
    }

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

    [Fact]
    public async Task CompareSignals_ReturnsDeterministicLoudestByRmsSummary()
    {
        var quietPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_quiet_{Guid.NewGuid():N}.wav");
        var loudPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_loud_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(quietPath, CreateMono16BitWav(sampleRate: 8, samples: [8192, 8192, 8192, 8192]));
        await File.WriteAllBytesAsync(loudPath, CreateMono16BitWav(sampleRate: 8, samples: [16384, 16384, 16384, 16384]));

        try
        {
            var quietFile = new ImportedFileSummary("compare-quiet.wav", new FileInfo(quietPath).Length, quietPath, "audio/wav");
            var loudFile = new ImportedFileSummary("compare-loud.wav", new FileInfo(loudPath).Length, loudPath, "audio/wav");
            var importedFiles = new[] { quietFile, loudFile };
            var dispatcher = BuildDispatcherWithFiles(importedFiles);
            var waveformService = new WaveformService();
            var baseline = waveformService.BuildTimeWaveforms(
                importedFiles,
                requestedBinCount: 64,
                selectedSignalIds: null,
                startTimeSeconds: null,
                endTimeSeconds: null,
                cancellationToken: CancellationToken.None);
            var signalIds = baseline.Recordings
                .SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            Assert.Equal(2, signalIds.Length);

            var compareJson = await dispatcher.DispatchAsync(
                AgentToolDefinitions.CompareSignals,
                $$"""{"signalIds":["{{signalIds[0]}}","{{signalIds[1]}}"]}""",
                CancellationToken.None);

            using var compareDoc = JsonDocument.Parse(compareJson);
            var loudestByRms = compareDoc.RootElement.GetProperty("loudestByRmsDbFs");

            Assert.Equal("compare-loud.wav", loudestByRms.GetProperty("fileName").GetString());
            Assert.Equal("Channel 1", loudestByRms.GetProperty("displayName").GetString());
            Assert.True(loudestByRms.GetProperty("rmsAmplitudeDbFs").GetDouble() >
                        compareDoc.RootElement.GetProperty("signals")[0].GetProperty("rmsAmplitudeDbFs").GetDouble());
        }
        finally
        {
            File.Delete(quietPath);
            File.Delete(loudPath);
        }
    }

    [Fact]
    public async Task CompareSignals_ReturnsAllSignalsAtHighestPeakWhenPeakIsTied()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_tie_a_{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_tie_b_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(firstPath, CreateMono16BitWav(sampleRate: 8, samples: [16384, 0, 0, 0]));
        await File.WriteAllBytesAsync(secondPath, CreateMono16BitWav(sampleRate: 8, samples: [16384, 8192, 8192, 8192]));

        try
        {
            var firstFile = new ImportedFileSummary("tie-a.wav", new FileInfo(firstPath).Length, firstPath, "audio/wav");
            var secondFile = new ImportedFileSummary("tie-b.wav", new FileInfo(secondPath).Length, secondPath, "audio/wav");
            var importedFiles = new[] { firstFile, secondFile };
            var dispatcher = BuildDispatcherWithFiles(importedFiles);
            var waveformService = new WaveformService();
            var baseline = waveformService.BuildTimeWaveforms(
                importedFiles,
                requestedBinCount: 64,
                selectedSignalIds: null,
                startTimeSeconds: null,
                endTimeSeconds: null,
                cancellationToken: CancellationToken.None);
            var signalIds = baseline.Recordings
                .SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            var compareJson = await dispatcher.DispatchAsync(
                AgentToolDefinitions.CompareSignals,
                $$"""{"signalIds":["{{signalIds[0]}}","{{signalIds[1]}}"]}""",
                CancellationToken.None);

            using var compareDoc = JsonDocument.Parse(compareJson);
            var peakTieSignals = compareDoc.RootElement.GetProperty("signalsAtHighestPeakDbFs").EnumerateArray().ToArray();

            Assert.Equal(2, peakTieSignals.Length);
            Assert.All(peakTieSignals, signal =>
            {
                var fileName = signal.GetProperty("fileName").GetString();
                Assert.True(fileName is "tie-a.wav" or "tie-b.wav");
                Assert.Equal(-6.0, signal.GetProperty("peakAmplitudeDbFs").GetDouble(), 1);
            });
            Assert.Equal(
                "The highest peak amplitude is tied at -6.0 dBFS across tie-a.wav · Channel 1, tie-b.wav · Channel 1.",
                compareDoc.RootElement.GetProperty("peakComparisonSummary").GetString());
            Assert.Equal(
                "No clipping was detected in any compared signal.",
                compareDoc.RootElement.GetProperty("clippingComparisonSummary").GetString());
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task CompareSignals_ReturnsSignalsWithClippingSummary()
    {
        var clippedPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_clipping_{Guid.NewGuid():N}.wav");
        var cleanPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_compare_clean_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(clippedPath, CreateMono16BitWav(sampleRate: 8, samples: [32767, 0, 0, 0]));
        await File.WriteAllBytesAsync(cleanPath, CreateMono16BitWav(sampleRate: 8, samples: [8192, 8192, 8192, 8192]));

        try
        {
            var clippedFile = new ImportedFileSummary("clip.wav", new FileInfo(clippedPath).Length, clippedPath, "audio/wav");
            var cleanFile = new ImportedFileSummary("clean.wav", new FileInfo(cleanPath).Length, cleanPath, "audio/wav");
            var importedFiles = new[] { clippedFile, cleanFile };
            var dispatcher = BuildDispatcherWithFiles(importedFiles);
            var waveformService = new WaveformService();
            var baseline = waveformService.BuildTimeWaveforms(
                importedFiles,
                requestedBinCount: 64,
                selectedSignalIds: null,
                startTimeSeconds: null,
                endTimeSeconds: null,
                cancellationToken: CancellationToken.None);
            var signalIds = baseline.Recordings
                .SelectMany(recording => recording.Signals)
                .Select(signal => signal.SignalId)
                .ToArray();

            var compareJson = await dispatcher.DispatchAsync(
                AgentToolDefinitions.CompareSignals,
                $$"""{"signalIds":["{{signalIds[0]}}","{{signalIds[1]}}"]}""",
                CancellationToken.None);

            using var compareDoc = JsonDocument.Parse(compareJson);
            var clippedSignals = compareDoc.RootElement.GetProperty("signalsWithClipping").EnumerateArray().ToArray();

            var clippedSignal = Assert.Single(clippedSignals);
            Assert.Equal("clip.wav", clippedSignal.GetProperty("fileName").GetString());
            Assert.Equal("Channel 1", clippedSignal.GetProperty("displayName").GetString());
            Assert.True(clippedSignal.GetProperty("hasClipping").GetBoolean());
            Assert.Equal(1, clippedSignal.GetProperty("clippingSampleCount").GetInt32());
            Assert.Equal(
                "Clipping was detected in clip.wav · Channel 1 (1 clipped samples).",
                compareDoc.RootElement.GetProperty("clippingComparisonSummary").GetString());
        }
        finally
        {
            File.Delete(clippedPath);
            File.Delete(cleanPath);
        }
    }
}
