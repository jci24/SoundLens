using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Comparisons.Common;

namespace SoundLens.Tests;

public sealed class RecordingComparisonTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly WebApplicationFactory<Program> _factory;

    static RecordingComparisonTests()
    {
        ResponseJsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    }

    public RecordingComparisonTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_RecordingComparison_ReturnsAlignedSignalsAndEffectiveRoi()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_a_{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_b_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(firstPath, CreateStereo16BitWav(
            sampleRate: 8,
            leftSamples: [-32768, -16384, 0, 16384],
            rightSamples: [32767, 16384, 0, -16384]));
        await File.WriteAllBytesAsync(secondPath, CreateStereo16BitWav(
            sampleRate: 8,
            leftSamples: [-32768, -16384, 0, 16384],
            rightSamples: [32767, 16384, 0, -16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { firstPath, secondPath } });
            importResponse.EnsureSuccessStatusCode();
            var waveformResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            waveformResponse.EnsureSuccessStatusCode();
            var imported = await waveformResponse.Content.ReadFromJsonAsync<ImportProbeResponse>();

            var response = await client.PostAsJsonAsync("/api/comparisons/recordings", new
            {
                recordingIdA = imported!.Recordings[0].RecordingId,
                recordingIdB = imported.Recordings[1].RecordingId,
                startTimeSeconds = 0.0,
                endTimeSeconds = 0.25
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RecordingComparisonResponse>(ResponseJsonOptions);

            Assert.NotNull(result);
            Assert.Equal(imported.Recordings[0].RecordingId, result!.RecordingA.RecordingId);
            Assert.Equal(imported.Recordings[1].RecordingId, result.RecordingB.RecordingId);
            Assert.Equal(2, result.AlignedSignals.Count);
            Assert.Equal(2, result.SignalObservations.Count);
            Assert.Equal(4, result.AggregateMetrics.Count);
            Assert.All(result.AlignedSignals, pair => Assert.Equal("DisplayName", pair.Basis.ToString()));
            Assert.Empty(result.Limitations);
            Assert.Equal("complete", result.IntegrityAssessment.Status);
            Assert.Equal(0, result.IntegrityAssessment.LimitedCheckCount);
            Assert.Equal(1, result.IntegrityAssessment.UnknownCheckCount);
            Assert.Equal("unknown", result.IntegrityAssessment.Checks.Single(check => check.Code == "Calibration").Status);
            Assert.NotNull(result.RegionOfInterest);
            Assert.Equal(0.0, result.RegionOfInterest!.StartTimeSeconds, precision: 4);
            Assert.Equal(0.25, result.RegionOfInterest.EndTimeSeconds, precision: 4);
            Assert.Equal(2, result.AggregateMetrics[0].ComparedPairCount);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task POST_RecordingComparison_ReturnsLimitationsForUnmatchedExtraSignals()
    {
        var monoPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_mono_{Guid.NewGuid():N}.wav");
        var stereoPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_stereo_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(monoPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [-32768, -16384, 0, 16384]));
        await File.WriteAllBytesAsync(stereoPath, CreateStereo16BitWav(
            sampleRate: 8,
            leftSamples: [-32768, -16384, 0, 16384],
            rightSamples: [32767, 16384, 0, -16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { monoPath, stereoPath } });
            importResponse.EnsureSuccessStatusCode();
            var waveformResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            waveformResponse.EnsureSuccessStatusCode();
            var imported = await waveformResponse.Content.ReadFromJsonAsync<ImportProbeResponse>();

            var response = await client.PostAsJsonAsync("/api/comparisons/recordings", new
            {
                recordingIdA = imported!.Recordings[0].RecordingId,
                recordingIdB = imported.Recordings[1].RecordingId
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RecordingComparisonResponse>(ResponseJsonOptions);

            Assert.NotNull(result);
            Assert.Single(result!.AlignedSignals);
            Assert.Single(result.SignalObservations);
            Assert.Equal(4, result.AggregateMetrics.Count);
            Assert.Equal(2, result.Limitations.Count);
            Assert.Equal("Missing", result.Limitations[0].Code);
            Assert.Equal("LowCoverage", result.Limitations[1].Code);
            Assert.Equal(1, result.AggregateMetrics[0].MissingValueCount);
            Assert.Equal("limited", result.IntegrityAssessment.Status);
            Assert.Equal("limited", result.IntegrityAssessment.Checks.Single(check => check.Code == "SignalAlignment").Status);
        }
        finally
        {
            File.Delete(monoPath);
            File.Delete(stereoPath);
        }
    }

    [Fact]
    public async Task POST_RecordingComparison_RejectsRoiThatExceedsShortestSelectedRecording()
    {
        var shortPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_short_{Guid.NewGuid():N}.wav");
        var longPath = Path.Combine(Path.GetTempPath(), $"soundlens_compare_long_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(shortPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));
        await File.WriteAllBytesAsync(longPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384, -32768, 32767, -16384, 16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { shortPath, longPath } });
            importResponse.EnsureSuccessStatusCode();
            var waveformResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            waveformResponse.EnsureSuccessStatusCode();
            var imported = await waveformResponse.Content.ReadFromJsonAsync<ImportProbeResponse>();

            var response = await client.PostAsJsonAsync("/api/comparisons/recordings", new
            {
                recordingIdA = imported!.Recordings[0].RecordingId,
                recordingIdB = imported.Recordings[1].RecordingId,
                startTimeSeconds = 0.0,
                endTimeSeconds = 1.5
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            File.Delete(shortPath);
            File.Delete(longPath);
        }
    }

    private sealed record ImportProbeResponse(IReadOnlyList<ProbeRecording> Recordings);
    private sealed record ProbeRecording(string RecordingId);

    private static byte[] CreateMono16BitWav(int sampleRate, IReadOnlyList<short> samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataSize = samples.Count * sizeof(short);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short));
        writer.Write((ushort)sizeof(short));
        writer.Write((ushort)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    private static byte[] CreateStereo16BitWav(
        int sampleRate,
        IReadOnlyList<short> leftSamples,
        IReadOnlyList<short> rightSamples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var frameCount = Math.Min(leftSamples.Count, rightSamples.Count);
        var dataSize = frameCount * sizeof(short) * 2;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)2);
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(short) * 2);
        writer.Write((ushort)(sizeof(short) * 2));
        writer.Write((ushort)16);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (var index = 0; index < frameCount; index++)
        {
            writer.Write(leftSamples[index]);
            writer.Write(rightSamples[index]);
        }

        return stream.ToArray();
    }
}
