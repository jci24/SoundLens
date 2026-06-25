using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Tests;

public sealed class FrequencySpectrumTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FrequencySpectrumTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_FrequencySpectra_ReturnsPeakNearKnownTone()
    {
        var sampleRate = 1024;
        var toneFrequencyHz = 128;
        var samples = CreateSineSamples(sampleRate, toneFrequencyHz, 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_spectrum_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/spectra/frequency", new { binCount = 513 });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<FrequencySpectrumResponse>();

            Assert.NotNull(result);
            var signal = Assert.Single(result!.SelectedSignals);
            var peak = signal.Points.MaxBy(point => point.Value);
            var lowerAdjacent = signal.Points.Single(point => point.FrequencyHz == toneFrequencyHz - 1);
            var upperAdjacent = signal.Points.Single(point => point.FrequencyHz == toneFrequencyHz + 1);

            Assert.NotNull(peak);
            Assert.Equal("Rectangular", result.Analysis.Window);
            Assert.Equal("Line spectrum", result.Analysis.Method);
            Assert.Equal(0, result.Analysis.OverlapPercent);
            Assert.Equal("dB rel.", result.Analysis.AmplitudeUnit);
            Assert.Equal(1, result.Analysis.FrequencyResolutionHz);
            Assert.InRange(peak!.FrequencyHz, toneFrequencyHz - 1.0, toneFrequencyHz + 1.0);
            Assert.True(peak.Value - lowerAdjacent.Value > 80);
            Assert.True(peak.Value - upperAdjacent.Value > 80);
            Assert.InRange(result.XAxis.Maximum, sampleRate / 2.0 - 0.1, sampleRate / 2.0 + 0.1);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_MatchesReferenceLineSpectrumImplementation()
    {
        var sampleRate = 510;
        var samples = CreateSineSamples(sampleRate, 17, 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_reference_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var importedFile = new ImportedFileSummary("reference.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 256, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var reference = BuildReferenceLineSpectrumDb(samples.Select(sample => sample / 32768.0).ToArray(), sampleRate, result.Analysis.FftLength);

            Assert.Equal(reference.Count, signal.Points.Count);
            Assert.Equal("Line spectrum", result.Analysis.Method);
            Assert.Equal("Rectangular", result.Analysis.Window);

            for (var index = 0; index < reference.Count; index++)
            {
                Assert.Equal(reference[index].frequencyHz, signal.Points[index].FrequencyHz, 6);
                Assert.Equal(reference[index].value, signal.Points[index].Value, 3);
            }
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_FrequencySpectra_ReturnsBadRequestBeforeImport()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/spectra/frequency", new { binCount = 256 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static List<(double frequencyHz, double value)> BuildReferenceLineSpectrumDb(
        IReadOnlyList<double> samples,
        int sampleRate,
        int fftLength)
    {
        var oneSidedBinCount = fftLength / 2 + 1;
        var accumulated = new double[oneSidedBinCount];
        var segments = 0;

        foreach (var start in EnumerateReferenceSegmentStarts(samples.Count, fftLength))
        {
            for (var bin = 0; bin < oneSidedBinCount; bin++)
            {
                var real = 0.0;
                var imaginary = 0.0;
                for (var n = 0; n < fftLength; n++)
                {
                    var angle = -2 * Math.PI * bin * n / fftLength;
                    real += samples[start + n] * Math.Cos(angle);
                    imaginary += samples[start + n] * Math.Sin(angle);
                }

                var amplitude = Math.Sqrt((real * real) + (imaginary * imaginary)) / fftLength;
                if (bin > 0 && bin < oneSidedBinCount - 1)
                {
                    amplitude *= 2;
                }

                accumulated[bin] += amplitude;
            }

            segments++;
        }

        var epsilon = 1e-12;
        return Enumerable.Range(0, oneSidedBinCount)
            .Select(bin =>
            {
                var averageAmplitude = accumulated[bin] / segments;
                return (
                    frequencyHz: bin * sampleRate / (double)fftLength,
                    value: 20 * Math.Log10(Math.Max(averageAmplitude, epsilon))
                );
            })
            .ToList();
    }

    private static IEnumerable<int> EnumerateReferenceSegmentStarts(int sampleCount, int fftLength)
    {
        for (var start = 0; start + fftLength <= sampleCount; start += fftLength)
        {
            yield return start;
        }
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
