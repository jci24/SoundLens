using System.Net;
using System.Net.Http.Json;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class TimeWaveformTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string MessagePackContentType = "application/x-msgpack";
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
    private readonly WebApplicationFactory<Program> _factory;

    public TimeWaveformTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsMinMaxBinsForImportedWav()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import",
                new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/waveforms/time",
                new { binCount = 64 });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(result);
            var recording = Assert.Single(result!.Recordings);
            var signal = Assert.Single(result.SelectedSignals);
            Assert.Equal(1, recording.Channels);
            Assert.Equal(4, signal.Bins.Count);
            Assert.Equal(4, signal.SampleRate);
            Assert.Equal(1.0, signal.DurationSeconds);
            Assert.Equal(-1.0, signal.Bins[0][0], precision: 4);
            Assert.Equal(32767 / 32768.0, signal.Bins[1][1], precision: 4);
            Assert.Equal(1.0, signal.Metrics.PeakAmplitude, precision: 4);
            Assert.InRange(signal.Metrics.RmsAmplitude, 0.79, 0.80);
            Assert.InRange(signal.Metrics.CrestFactor, 1.25, 1.27);
            Assert.Equal(2, signal.Metrics.ClippingSampleCount);
            Assert.True(signal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsMessagePackWhenRequested()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_msgpack_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/waveforms/time")
            {
                Content = JsonContent.Create(new { binCount = 64 })
            };
            request.Headers.Accept.ParseAdd(MessagePackContentType);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Assert.Equal(MessagePackContentType, response.Content.Headers.ContentType?.MediaType);

            var payload = await response.Content.ReadAsByteArrayAsync();
            var result = MessagePackSerializer.Deserialize<TimeWaveformResponse>(payload, MessagePackOptions);

            var signal = Assert.Single(result.SelectedSignals);
            Assert.Equal(4, signal.Bins.Count);
            Assert.Equal(1.0, signal.Metrics.PeakAmplitude, precision: 4);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsBadRequestBeforeImport()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/waveforms/time",
            new { binCount = 256 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_TimeWaveforms_RejectsOutOfRangeBinCount()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/waveforms/time",
            new { binCount = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsBadRequestWhenAllFilesFailToDecode()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_invalid_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, [0x00, 0x01, 0x02]);

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import",
                new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/waveforms/time",
                new { binCount = 64 });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsRequestedStereoSignal()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_stereo_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateStereo16BitWav(
            sampleRate: 8,
            leftSamples: [-32768, -16384, 0, 16384],
            rightSamples: [32767, 16384, 0, -16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var baselineResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            baselineResponse.EnsureSuccessStatusCode();
            var baseline = await baselineResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(baseline);
            var recording = Assert.Single(baseline!.Recordings);
            var selectedSignal = recording.Signals.Single(signalSummary => signalSummary.ChannelIndex == 1);
            var selectedSignalId = selectedSignal.SignalId;

            var filteredResponse = await client.PostAsJsonAsync("/api/waveforms/time", new
            {
                binCount = 64,
                signalIds = new[] { selectedSignalId }
            });

            filteredResponse.EnsureSuccessStatusCode();
            var filtered = await filteredResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(filtered);
            var signal = Assert.Single(filtered!.SelectedSignals);
            Assert.Equal(selectedSignalId, signal.SignalId);
            Assert.Equal(1, signal.ChannelIndex);
            Assert.Equal(32767 / 32768.0, signal.Bins[0][1], precision: 4);
            Assert.Equal(32767 / 32768.0, signal.Metrics.PeakAmplitude, precision: 4);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_ReusesCachedWaveformAfterSourceFileIsDeleted()
    {
        var sampleRate = 1024;
        var samples = CreateSineSamples(sampleRate, frequencyHz: 128, durationSeconds: 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_cache_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        var importedFile = new ImportedFileSummary("cached-waveform.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
        var waveformService = new WaveformService();

        try
        {
            var firstResult = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, CancellationToken.None);
            File.Delete(tempPath);

            var secondResult = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, CancellationToken.None);
            Assert.Equal(firstResult.Recordings.Count, secondResult.Recordings.Count);
            Assert.Equal(firstResult.SelectedSignals.Count, secondResult.SelectedSignals.Count);
            Assert.Equal(firstResult.YAxis.Unit, secondResult.YAxis.Unit);
            Assert.Equal(firstResult.YAxis.Minimum, secondResult.YAxis.Minimum, 6);
            Assert.Equal(firstResult.YAxis.Maximum, secondResult.YAxis.Maximum, 6);
            Assert.Equal(firstResult.YAxis.Ticks, secondResult.YAxis.Ticks);

            var firstRecording = Assert.Single(firstResult.Recordings);
            var secondRecording = Assert.Single(secondResult.Recordings);
            Assert.Equal(firstRecording.RecordingId, secondRecording.RecordingId);
            Assert.Equal(firstRecording.FileName, secondRecording.FileName);
            Assert.Equal(firstRecording.DurationSeconds, secondRecording.DurationSeconds, 6);
            Assert.Equal(firstRecording.Signals.Count, secondRecording.Signals.Count);

            var firstSignal = Assert.Single(firstResult.SelectedSignals);
            var secondSignal = Assert.Single(secondResult.SelectedSignals);
            Assert.Equal(firstSignal.SignalId, secondSignal.SignalId);
            Assert.Equal(firstSignal.Bins.Count, secondSignal.Bins.Count);
            Assert.Equal(firstSignal.Bins[0], secondSignal.Bins[0]);
            Assert.Equal(firstSignal.Bins[^1], secondSignal.Bins[^1]);
            Assert.Equal(firstSignal.Metrics, secondSignal.Metrics);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReportsExpectedMetricsForSilenceAndClippedSignal()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_metrics_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 1024,
            samples: CreateHardClippedSineSamples(1024, frequencyHz: 64, durationSeconds: 2.0)));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 256 });
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            var signal = Assert.Single(result!.SelectedSignals);

            Assert.True(signal.Metrics.PeakAmplitude > 0.54);
            Assert.True(signal.Metrics.RmsAmplitude > 0.44);
            Assert.True(signal.Metrics.CrestFactor < 1.3);
            Assert.True(signal.Metrics.ClippingSampleCount > 0);
            Assert.True(signal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static byte[] CreateMono16BitWav(int sampleRate, IReadOnlyList<short> samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Count * sizeof(short);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    private static short[] CreateSineSamples(int sampleRate, int frequencyHz, double durationSeconds)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var samples = new short[sampleCount];

        for (var index = 0; index < sampleCount; index++)
        {
            var value = Math.Sin((2 * Math.PI * frequencyHz * index) / sampleRate) * 0.8;
            samples[index] = (short)Math.Round(value * short.MaxValue);
        }

        return samples;
    }

    private static short[] CreateHardClippedSineSamples(int sampleRate, int frequencyHz, double durationSeconds)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var samples = new short[sampleCount];

        for (var index = 0; index < sampleCount; index++)
        {
            var rawValue = Math.Sin((2 * Math.PI * frequencyHz * index) / sampleRate) * 1.3;
            var clippedValue = Math.Clamp(rawValue, -1.0, 1.0);
            samples[index] = (short)Math.Round(clippedValue * short.MaxValue);
        }

        return samples;
    }

    private static byte[] CreateStereo16BitWav(
        int sampleRate,
        IReadOnlyList<short> leftSamples,
        IReadOnlyList<short> rightSamples)
    {
        if (leftSamples.Count != rightSamples.Count)
        {
            throw new ArgumentException("Stereo channels must have the same sample count.");
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = leftSamples.Count * sizeof(short) * 2;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)2);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short) * 2);
        writer.Write((short)(sizeof(short) * 2));
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        for (var index = 0; index < leftSamples.Count; index++)
        {
            writer.Write(leftSamples[index]);
            writer.Write(rightSamples[index]);
        }

        return stream.ToArray();
    }
}
