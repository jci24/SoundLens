using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Tests;

public sealed class TimeWaveformTests : IClassFixture<WebApplicationFactory<Program>>
{
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
            Assert.Equal(4, signal.Points.Count);
            Assert.Equal(4, signal.SampleRate);
            Assert.Equal(1.0, signal.DurationSeconds);
            Assert.Equal(-1.0, signal.Points[0].MinAmplitude, precision: 4);
            Assert.Equal(32767 / 32768.0, signal.Points[1].MaxAmplitude, precision: 4);
            Assert.Equal(0.5, signal.Points[2].TimeSeconds, precision: 4);
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
}
