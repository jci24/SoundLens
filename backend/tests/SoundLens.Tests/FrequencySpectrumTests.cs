using System.Net;
using System.Net.Http.Json;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Tests;

public sealed class FrequencySpectrumTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string MessagePackContentType = "application/x-msgpack";
    private static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);
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
            Assert.InRange(signal.Metrics.PeakAmplitude, 0.79, 0.81);
            Assert.InRange(signal.Metrics.RmsAmplitude, 0.56, 0.57);
            Assert.InRange(signal.Metrics.CrestFactor, 1.41, 1.42);
            Assert.Equal(0, signal.Metrics.ClippingSampleCount);
            Assert.False(signal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_FrequencySpectra_ReturnsMessagePackWhenRequested()
    {
        var sampleRate = 1024;
        var toneFrequencyHz = 128;
        var samples = CreateSineSamples(sampleRate, toneFrequencyHz, 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_spectrum_msgpack_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/spectra/frequency")
            {
                Content = JsonContent.Create(new { binCount = 513 })
            };
            request.Headers.Accept.ParseAdd(MessagePackContentType);

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Assert.Equal(MessagePackContentType, response.Content.Headers.ContentType?.MediaType);

            var payload = await response.Content.ReadAsByteArrayAsync();
            var result = MessagePackSerializer.Deserialize<FrequencySpectrumResponse>(payload, MessagePackOptions);

            var signal = Assert.Single(result.SelectedSignals);
            var peak = signal.Points.MaxBy(point => point.Value);

            Assert.NotNull(peak);
            Assert.InRange(peak!.FrequencyHz, toneFrequencyHz - 1.0, toneFrequencyHz + 1.0);
            Assert.Equal("Line spectrum", result.Analysis.Method);
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

    [Fact]
    public async Task POST_FrequencySpectra_ReturnsRequestedStereoSignal()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_spectrum_stereo_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateStereo16BitWav(
            sampleRate: 1024,
            leftSamples: CreateSineSamples(1024, 64, 2.0),
            rightSamples: CreateSineSamples(1024, 128, 2.0)));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var baselineResponse = await client.PostAsJsonAsync("/api/spectra/frequency", new { binCount = 513 });
            baselineResponse.EnsureSuccessStatusCode();
            var baseline = await baselineResponse.Content.ReadFromJsonAsync<FrequencySpectrumResponse>();

            Assert.NotNull(baseline);
            var recording = Assert.Single(baseline!.Recordings);
            var selectedSignal = recording.Signals.Single(signalSummary => signalSummary.ChannelIndex == 1);
            var selectedSignalId = selectedSignal.SignalId;

            var filteredResponse = await client.PostAsJsonAsync("/api/spectra/frequency", new
            {
                binCount = 513,
                signalIds = new[] { selectedSignalId }
            });

            filteredResponse.EnsureSuccessStatusCode();
            var filtered = await filteredResponse.Content.ReadFromJsonAsync<FrequencySpectrumResponse>();

            Assert.NotNull(filtered);
            var signal = Assert.Single(filtered!.SelectedSignals);
            var peak = signal.Points.MaxBy(point => point.Value);

            Assert.Equal(selectedSignalId, signal.SignalId);
            Assert.Equal(1, signal.ChannelIndex);
            Assert.NotNull(peak);
            Assert.InRange(peak!.FrequencyHz, 127.0, 129.0);
            Assert.InRange(signal.Metrics.PeakAmplitude, 0.79, 0.81);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_ReportsFilesExceedingMaximumFrameLimitAsFailures()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_large_header_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateHeaderOnlyMono16BitWav(sampleRate: 48000, declaredFrameCount: 10_000_001));

        try
        {
            var importedFile = new ImportedFileSummary("too-large.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 256, selectedSignalIds: null, CancellationToken.None);

            Assert.Empty(result.Recordings);
            Assert.Empty(result.SelectedSignals);
            Assert.Equal(["too-large.wav"], result.FailedFiles);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_ReturnsStableNoiseFloorForSilence()
    {
        var sampleRate = 1024;
        var samples = new short[sampleRate * 2];
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_silence_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var importedFile = new ImportedFileSummary("silence.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);

            Assert.All(signal.Points, point => Assert.Equal(-240, point.Value, 6));
            Assert.Equal(-240, result.YAxis.Minimum, 6);
            Assert.Equal(-237, result.YAxis.Maximum, 6);
            Assert.Equal(0, signal.Metrics.PeakAmplitude, 6);
            Assert.Equal(0, signal.Metrics.RmsAmplitude, 6);
            Assert.Equal(0, signal.Metrics.CrestFactor, 6);
            Assert.Equal(0, signal.Metrics.ClippingSampleCount);
            Assert.False(signal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_ReturnsTwoDominantPeaksForDualToneSignal()
    {
        var sampleRate = 1024;
        var samples = CreateDualToneSamples(sampleRate, firstFrequencyHz: 128, secondFrequencyHz: 256, durationSeconds: 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_dualtone_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var importedFile = new ImportedFileSummary("dual-tone.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var topPeaks = signal.Points
                .OrderByDescending(point => point.Value)
                .Take(2)
                .Select(point => point.FrequencyHz)
                .Order()
                .ToArray();

            Assert.Equal([128d, 256d], topPeaks);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_ShowsOddHarmonicsForHardClippedSine()
    {
        var sampleRate = 1024;
        var fundamentalFrequencyHz = 64;
        var samples = CreateHardClippedSineSamples(sampleRate, fundamentalFrequencyHz, durationSeconds: 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_clipped_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var importedFile = new ImportedFileSummary("clipped.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var fundamental = signal.Points.Single(point => point.FrequencyHz == fundamentalFrequencyHz);
            var thirdHarmonic = signal.Points.Single(point => point.FrequencyHz == fundamentalFrequencyHz * 3);
            var fifthHarmonic = signal.Points.Single(point => point.FrequencyHz == fundamentalFrequencyHz * 5);
            var adjacentThirdLower = signal.Points.Single(point => point.FrequencyHz == (fundamentalFrequencyHz * 3) - 1);
            var adjacentThirdUpper = signal.Points.Single(point => point.FrequencyHz == (fundamentalFrequencyHz * 3) + 1);

            Assert.True(fundamental.Value > thirdHarmonic.Value);
            Assert.True(thirdHarmonic.Value > fifthHarmonic.Value);
            Assert.True(thirdHarmonic.Value - adjacentThirdLower.Value > 40);
            Assert.True(thirdHarmonic.Value - adjacentThirdUpper.Value > 40);
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

    [Fact]
    public async Task SpectrumService_ReusesCachedSpectrumAfterSourceFileIsDeleted()
    {
        var sampleRate = 1024;
        var samples = CreateSineSamples(sampleRate, frequencyHz: 128, durationSeconds: 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_spectrum_cache_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        var importedFile = new ImportedFileSummary("cached-spectrum.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
        var spectrumService = new SpectrumService();

        try
        {
            var firstResult = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);
            File.Delete(tempPath);

            var secondResult = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);
            Assert.Equal(firstResult.Analysis, secondResult.Analysis);
            Assert.Equal(firstResult.Recordings.Count, secondResult.Recordings.Count);
            Assert.Equal(firstResult.SelectedSignals.Count, secondResult.SelectedSignals.Count);
            Assert.Equal(firstResult.XAxis.Unit, secondResult.XAxis.Unit);
            Assert.Equal(firstResult.XAxis.Minimum, secondResult.XAxis.Minimum, 6);
            Assert.Equal(firstResult.XAxis.Maximum, secondResult.XAxis.Maximum, 6);
            Assert.Equal(firstResult.XAxis.Ticks, secondResult.XAxis.Ticks);
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
            Assert.Equal(firstSignal.Metrics, secondSignal.Metrics);
            Assert.Equal(firstSignal.Points.Count, secondSignal.Points.Count);
            Assert.Equal(firstSignal.Points[0], secondSignal.Points[0]);
            Assert.Equal(firstSignal.Points[^1], secondSignal.Points[^1]);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_DCSignalConcentratesEnergyAtZeroHz()
    {
        // A pure DC signal (all samples equal, non-zero) should produce a spectrum
        // where the 0 Hz bin has the highest value and all other bins are far below it.
        // This is distinct from silence: DC has measurable energy, just all at 0 Hz.
        const short dcSampleValue = 16384; // 0.5 FS
        var samples = Enumerable.Repeat(dcSampleValue, 2048).ToArray();
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_dc_spectrum_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate: 2048, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("dc.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 1025, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var dcBin = signal.Points.Single(point => point.FrequencyHz == 0);
            var dcValue = dcBin.Value;

            // The DC bin should be substantially higher than any AC bin
            var maxAcValue = signal.Points
                .Where(point => point.FrequencyHz > 0)
                .Max(point => point.Value);

            Assert.True(dcValue > maxAcValue + 60,
                $"Expected DC bin ({dcValue:F1} dB) to dominate AC bins (max {maxAcValue:F1} dB) by at least 60 dB");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_NyquistBinIsPresentAtHalfSampleRate()
    {
        // The one-sided spectrum should include a point at exactly sampleRate / 2 Hz (Nyquist).
        // This verifies that the upper edge bin is always present in the output.
        var sampleRate = 1024;
        var samples = CreateSineSamples(sampleRate, frequencyHz: 64, durationSeconds: 2.0);
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_nyquist_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, samples));

        try
        {
            var importedFile = new ImportedFileSummary("nyquist.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var nyquistHz = sampleRate / 2.0;

            // The spectrum must contain a point at the Nyquist frequency
            var nyquistBin = signal.Points.SingleOrDefault(point => Math.Abs(point.FrequencyHz - nyquistHz) < 0.01);
            Assert.NotNull(nyquistBin);

            // The x-axis maximum should equal the Nyquist frequency
            Assert.InRange(result.XAxis.Maximum, nyquistHz - 0.1, nyquistHz + 0.1);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task SpectrumService_SignalShorterThanRequestedFftLengthGraceFullyDegrades()
    {
        // When a file has fewer samples than the requested FFT length, the service clamps
        // fftLength to the actual sample count rather than failing. This means a signal
        // with N samples still produces a spectrum using an N-point FFT.
        // This test verifies the graceful degradation: the result must be a valid signal
        // with spectrum points, and the frequency resolution reflects the clamped FFT size.
        var sampleRate = 1024;
        var shortSamples = new short[] { 1000, -1000, 500, -500 }; // 4 samples; fftLength will be clamped to 4
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_short_signal_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate, shortSamples));

        try
        {
            var importedFile = new ImportedFileSummary("short.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var spectrumService = new SpectrumService();

            // binCount = 513 implies requestedFftLength = 1024; sample count (4) < 1024
            // so fftLength is clamped to 4, giving a 3-point one-sided spectrum (4/2 + 1)
            var result = spectrumService.BuildFrequencySpectra([importedFile], requestedBinCount: 513, selectedSignalIds: null, CancellationToken.None);

            Assert.Empty(result.FailedFiles);
            var signal = Assert.Single(result.SelectedSignals);

            // A 4-point FFT produces 3 one-sided bins (0 Hz, Nyquist/2, Nyquist)
            Assert.Equal(3, signal.Points.Count);

            // Frequency resolution = sampleRate / fftLength = 1024 / 4 = 256 Hz
            Assert.Equal(256.0, result.Analysis.FrequencyResolutionHz, precision: 6);
        }
        finally
        {
            File.Delete(tempPath);
        }
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

    private static short[] CreateDualToneSamples(int sampleRate, int firstFrequencyHz, int secondFrequencyHz, double durationSeconds)
    {
        var sampleCount = (int)(sampleRate * durationSeconds);
        var samples = new short[sampleCount];

        for (var index = 0; index < sampleCount; index++)
        {
            var firstTone = Math.Sin((2 * Math.PI * firstFrequencyHz * index) / sampleRate) * 0.4;
            var secondTone = Math.Sin((2 * Math.PI * secondFrequencyHz * index) / sampleRate) * 0.4;
            samples[index] = (short)Math.Round((firstTone + secondTone) * short.MaxValue);
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

    private static byte[] CreateHeaderOnlyMono16BitWav(int sampleRate, int declaredFrameCount)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        // Intentionally declare more data than is present so the decoder hits
        // the oversized-frame guardrail through its normal file-read path.
        var declaredDataLength = declaredFrameCount * sizeof(short);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + declaredDataLength);
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
        writer.Write(declaredDataLength);

        return stream.ToArray();
    }
}
