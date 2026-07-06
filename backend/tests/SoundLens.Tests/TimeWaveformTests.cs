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
            Assert.Null(result.RegionOfInterest);
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
    public async Task POST_TimeWaveforms_PreservesRequestedSignalOrderAcrossRecordings()
    {
        var firstPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_first_{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_second_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(firstPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [32767, 16384, 0, -16384]));
        await File.WriteAllBytesAsync(secondPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [-32768, -16384, 0, 16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { firstPath, secondPath } });
            importResponse.EnsureSuccessStatusCode();

            var baselineResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            baselineResponse.EnsureSuccessStatusCode();
            var baseline = await baselineResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(baseline);
            Assert.Equal(2, baseline!.Recordings.Count);

            var firstSignalId = baseline.Recordings[0].Signals.Single().SignalId;
            var secondSignalId = baseline.Recordings[1].Signals.Single().SignalId;

            var filteredResponse = await client.PostAsJsonAsync("/api/waveforms/time", new
            {
                binCount = 64,
                signalIds = new[] { secondSignalId, firstSignalId }
            });

            filteredResponse.EnsureSuccessStatusCode();
            var filtered = await filteredResponse.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(filtered);
            Assert.Equal([secondSignalId, firstSignalId], filtered!.SelectedSignals.Select(signal => signal.SignalId).ToArray());
            Assert.Equal(baseline.Recordings[1].FileName, filtered.SelectedSignals[0].RecordingFileName);
            Assert.Equal(baseline.Recordings[0].FileName, filtered.SelectedSignals[1].RecordingFileName);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_ReturnsRegionScopedMetricsAndEchoesRequestedRegion()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_roi_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [32767, 32767, 32767, 32767, 8192, 8192, 8192, 8192]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/waveforms/time", new
            {
                binCount = 64,
                startTimeSeconds = 0.5,
                endTimeSeconds = 1.0
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TimeWaveformResponse>();

            Assert.NotNull(result);
            var signal = Assert.Single(result!.SelectedSignals);
            Assert.NotNull(result.RegionOfInterest);
            Assert.Equal(0.5, result.RegionOfInterest!.StartTimeSeconds, 6);
            Assert.Equal(1.0, result.RegionOfInterest.EndTimeSeconds, 6);
            Assert.Equal(0.5, result.RegionOfInterest.DurationSeconds, 6);
            Assert.InRange(signal.Metrics.PeakAmplitude, 0.24, 0.26);
            Assert.All(signal.Bins, bin => Assert.InRange(bin[1], 0.24, 0.26));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task POST_TimeWaveforms_RejectsRegionThatExceedsSelectedSignalDuration()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_waveform_roi_invalid_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 8,
            samples: [32767, 16384, 0, -16384]));

        try
        {
            var client = _factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync("/api/waveforms/time", new
            {
                binCount = 64,
                startTimeSeconds = 0.25,
                endTimeSeconds = 1.5
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            var firstResult = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);
            File.Delete(tempPath);

            var secondResult = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);
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
    public async Task WaveformService_Reads8BitPcmCorrectly()
    {
        // 8-bit PCM normalises as (byte - 128) / 128.0.
        // Byte value 200 -> (200 - 128) / 128.0 = 72/128  = 0.5625  (below positive full scale)
        // Byte value 64  -> ( 64 - 128) / 128.0 = -64/128 = -0.5    (above negative full scale)
        // Byte value 128 -> (128 - 128) / 128.0 = 0.0
        // Note: byte 255 equals the positive full-scale threshold (127/128) so it IS clipped.
        //       byte 0 equals -1.0 (negative full scale) and also IS clipped.
        //       We deliberately stay within the non-clipping range here.
        var samples = new byte[] { 200, 64, 128, 160 };
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_8bit_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono8BitWav(sampleRate: 4, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("8bit.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var waveformService = new WaveformService();

            var result = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            Assert.Equal(4, signal.Bins.Count);

            // Bin 0: only sample 200 -> normalised 72/128 = 0.5625; min and max both equal that value
            Assert.Equal(72 / 128.0, signal.Bins[0][0], precision: 6);
            Assert.Equal(72 / 128.0, signal.Bins[0][1], precision: 6);

            // Bin 1: only sample 64 -> normalised -64/128 = -0.5
            Assert.Equal(-64 / 128.0, signal.Bins[1][0], precision: 6);
            Assert.Equal(-64 / 128.0, signal.Bins[1][1], precision: 6);

            // Bin 2: only sample 128 -> normalised 0.0
            Assert.Equal(0.0, signal.Bins[2][0], precision: 6);
            Assert.Equal(0.0, signal.Bins[2][1], precision: 6);

            // Peak is 72/128 = 0.5625 (sample 200); well within range so no clipping
            Assert.Equal(72 / 128.0, signal.Metrics.PeakAmplitude, precision: 6);
            Assert.False(signal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_Reads24BitPcmCorrectly()
    {
        // 24-bit PCM normalises as ReadInt24(reader) / 8388608.0.
        // Full positive: 8388607 -> 8388607 / 8388608.0 ≈ 0.999999881
        // Full negative: -8388608 -> -8388608 / 8388608.0 = -1.0
        // Zero: 0 -> 0.0
        // This exercises the sign-extension branch in ReadInt24.
        var samples = new int[] { 8388607, -8388608, 0, 4194304 };
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_24bit_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono24BitWav(sampleRate: 4, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("24bit.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var waveformService = new WaveformService();

            var result = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            Assert.Equal(4, signal.Bins.Count);

            // Bin 0: 8388607 / 8388608.0
            Assert.Equal(8388607 / 8388608.0, signal.Bins[0][0], precision: 5);

            // Bin 1: -8388608 / 8388608.0 = -1.0
            Assert.Equal(-1.0, signal.Bins[1][0], precision: 6);

            // Bin 2: 0.0
            Assert.Equal(0.0, signal.Bins[2][0], precision: 6);

            // Peak amplitude should reflect the positive full-scale sample
            Assert.Equal(8388607 / 8388608.0, signal.Metrics.PeakAmplitude, precision: 5);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_Reads32BitFloatWavCorrectly()
    {
        // IEEE 32-bit float WAV (audioFormat = 3). Values are read directly as floats.
        // Clipping threshold is 1.0 for float WAV.
        var samples = new float[] { 0.5f, -0.5f, 0.0f, 1.0f };
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_float32_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono32BitFloatWav(sampleRate: 4, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("float32.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var waveformService = new WaveformService();

            var result = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            Assert.Equal(4, signal.Bins.Count);

            Assert.Equal(0.5, signal.Bins[0][0], precision: 5);
            Assert.Equal(-0.5, signal.Bins[1][0], precision: 5);
            Assert.Equal(0.0, signal.Bins[2][0], precision: 5);

            // Sample 1.0 equals the full-scale threshold for float WAV so it counts as clipping
            Assert.Equal(1, signal.Metrics.ClippingSampleCount);
            Assert.True(signal.Metrics.HasClipping);
            Assert.Equal(1.0, signal.Metrics.PeakAmplitude, precision: 5);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_DCSignalProducesCorrectMetrics()
    {
        // A pure DC signal (all samples equal) should give:
        //   peak = RMS = constant value
        //   crest factor = 1.0  (peak / RMS)
        //   no clipping (value is below full scale)
        //   all bin min == bin max (flat waveform)
        const short dcSampleValue = 16384; // 0.5 FS
        var samples = Enumerable.Repeat(dcSampleValue, 256).ToArray();
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_dc_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate: 256, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("dc.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var waveformService = new WaveformService();

            var result = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 64, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var expectedNormalised = dcSampleValue / 32768.0;

            Assert.Equal(expectedNormalised, signal.Metrics.PeakAmplitude, precision: 5);
            Assert.Equal(expectedNormalised, signal.Metrics.RmsAmplitude, precision: 5);
            Assert.Equal(1.0, signal.Metrics.CrestFactor, precision: 4);
            Assert.Equal(0, signal.Metrics.ClippingSampleCount);
            Assert.False(signal.Metrics.HasClipping);

            // Every bin min and max should be equal because the waveform is flat
            Assert.All(signal.Bins, bin => Assert.Equal(bin[0], bin[1], precision: 6));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_BinEnvelopeContainsMinAndMaxForCrossingSignal()
    {
        // A signal that alternates between a large positive and large negative value
        // within the same bin should produce bin[0] (min) < 0 and bin[1] (max) > 0.
        // This verifies the BinAccumulator tracks both extremes, not just one.
        var samples = new short[128];
        for (var i = 0; i < samples.Length; i++)
        {
            samples[i] = i % 2 == 0 ? (short)24576 : (short)-24576; // ±0.75 FS
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_crossing_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(sampleRate: 128, samples: samples));

        try
        {
            var importedFile = new ImportedFileSummary("crossing.wav", new FileInfo(tempPath).Length, tempPath, "audio/wav");
            var waveformService = new WaveformService();

            // Request only 1 bin so all samples land in the same bin
            var result = waveformService.BuildTimeWaveforms([importedFile], requestedBinCount: 64, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);

            var signal = Assert.Single(result.SelectedSignals);
            var firstBin = signal.Bins[0];

            Assert.True(firstBin[0] < 0, $"Expected bin min < 0 but was {firstBin[0]}");
            Assert.True(firstBin[1] > 0, $"Expected bin max > 0 but was {firstBin[1]}");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task WaveformService_ClippingThresholdIsPrecise()
    {
        // short.MaxValue (32767) equals the positive full-scale threshold for 16-bit PCM.
        // A sample at 32767 should be counted as clipping.
        // A sample at 32766 should NOT be counted as clipping.
        var clippingSamples = new short[] { 32767, 0, 0, 0 };
        var safesamples = new short[] { 32766, 0, 0, 0 };

        var clippingPath = Path.Combine(Path.GetTempPath(), $"soundlens_clip_boundary_hi_{Guid.NewGuid():N}.wav");
        var safePath = Path.Combine(Path.GetTempPath(), $"soundlens_clip_boundary_lo_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(clippingPath, CreateMono16BitWav(sampleRate: 4, samples: clippingSamples));
        await File.WriteAllBytesAsync(safePath, CreateMono16BitWav(sampleRate: 4, samples: safesamples));

        try
        {
            var waveformService = new WaveformService();

            var clippingFile = new ImportedFileSummary("clip.wav", new FileInfo(clippingPath).Length, clippingPath, "audio/wav");
            var clippingResult = waveformService.BuildTimeWaveforms([clippingFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);
            var clippingSignal = Assert.Single(clippingResult.SelectedSignals);

            Assert.Equal(1, clippingSignal.Metrics.ClippingSampleCount);
            Assert.True(clippingSignal.Metrics.HasClipping);

            var safeFile = new ImportedFileSummary("safe.wav", new FileInfo(safePath).Length, safePath, "audio/wav");
            var safeResult = waveformService.BuildTimeWaveforms([safeFile], requestedBinCount: 256, selectedSignalIds: null, startTimeSeconds: null, endTimeSeconds: null, CancellationToken.None);
            var safeSignal = Assert.Single(safeResult.SelectedSignals);

            Assert.Equal(0, safeSignal.Metrics.ClippingSampleCount);
            Assert.False(safeSignal.Metrics.HasClipping);
        }
        finally
        {
            File.Delete(clippingPath);
            File.Delete(safePath);
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

    private static byte[] CreateMono8BitWav(int sampleRate, IReadOnlyList<byte> samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Count * sizeof(byte);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);           // PCM
        writer.Write((short)1);           // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(byte));
        writer.Write((short)sizeof(byte));
        writer.Write((short)8);           // 8-bit
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    private static byte[] CreateMono24BitWav(int sampleRate, IReadOnlyList<int> samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var bytesPerSample = 3;
        var dataLength = samples.Count * bytesPerSample;

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);                       // PCM
        writer.Write((short)1);                       // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * bytesPerSample);
        writer.Write((short)bytesPerSample);
        writer.Write((short)24);                      // 24-bit
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            // Write little-endian 24-bit two's complement
            writer.Write((byte)(sample & 0xFF));
            writer.Write((byte)((sample >> 8) & 0xFF));
            writer.Write((byte)((sample >> 16) & 0xFF));
        }

        return stream.ToArray();
    }

    private static byte[] CreateMono32BitFloatWav(int sampleRate, IReadOnlyList<float> samples)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var dataLength = samples.Count * sizeof(float);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataLength);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)3);                    // IEEE float
        writer.Write((short)1);                    // mono
        writer.Write(sampleRate);
        writer.Write(sampleRate * sizeof(float));
        writer.Write((short)sizeof(float));
        writer.Write((short)32);                   // 32-bit
        writer.Write("data"u8.ToArray());
        writer.Write(dataLength);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
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
