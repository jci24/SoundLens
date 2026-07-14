using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;

namespace SoundLens.Tests;

public sealed class ExportComparisonReportMarkdownTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportComparisonReportMarkdownTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IComparisonReportNarrativeService>();
            services.AddSingleton<IComparisonReportNarrativeService>(new StubComparisonNarrativeService());
        }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task POST_ComparisonReport_ReconstructsEvidenceAndExportsMarkdown(bool useRoi)
    {
        await using var fixture = await ImportComparisonFixture.CreateAsync(_factory);
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(
                excludedRecordings:
                [
                    new
                    {
                        recordingId = fixture.Recordings[2].RecordingId,
                        assignment = "unassigned"
                    }
                ],
                useRoi: useRoi));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ExportReportMarkdownResponse>();

        Assert.NotNull(payload);
        Assert.EndsWith(".md", payload!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("# alpha.wav vs beta.wav comparison", payload.Markdown);
        Assert.Contains("## Comparison Scope", payload.Markdown);
        Assert.Contains("- Compare A: alpha.wav", payload.Markdown);
        Assert.Contains("- Compare B: beta.wav", payload.Markdown);
        Assert.Contains(useRoi ? "- Region: 0 s to 0.5 s (0.5 s)" : "- Region: full duration", payload.Markdown);
        Assert.Contains("## Ranked Differences", payload.Markdown);
        Assert.Contains("| 1 |", payload.Markdown);
        Assert.Contains("## Selected Evidence", payload.Markdown);
        Assert.Contains("### RMS amplitude", payload.Markdown);
        Assert.Contains("A-B", payload.Markdown);
        Assert.Contains("## AI Interpretation", payload.Markdown);
        Assert.Contains("Aggregate RMS evidence is lower for Compare A. The selected aligned pair supports that direction.", payload.Markdown);
        Assert.Contains("gamma.wav - Unassigned; excluded because this report covers only the active A/B pair.", payload.Markdown);
        Assert.Contains("## Limitations", payload.Markdown);
        Assert.Contains("not calibrated physical SPL", payload.Markdown);
        Assert.Contains("## Traceability", payload.Markdown);
        Assert.Contains(fixture.Recordings[0].RecordingId, payload.Markdown);
        Assert.Contains(fixture.Recordings[1].RecordingId, payload.Markdown);
    }

    [Fact]
    public async Task POST_ComparisonReport_RejectsInvalidSelectionAndExclusions()
    {
        await using var fixture = await ImportComparisonFixture.CreateAsync(_factory);
        var activeId = fixture.Recordings[0].RecordingId;
        var excludedId = fixture.Recordings[2].RecordingId;

        var invalidMetric = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(metricKey: "inventedMetric"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidMetric.StatusCode);

        var invalidPair = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(signalIdB: "not-an-aligned-signal"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPair.StatusCode);

        var duplicateExclusion = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = excludedId, assignment = "A" },
                new { recordingId = excludedId, assignment = "B" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateExclusion.StatusCode);

        var activeExclusion = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = activeId, assignment = "A" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, activeExclusion.StatusCode);

        var unknownExclusion = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = "missing-recording", assignment = "unassigned" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, unknownExclusion.StatusCode);

        var malformedAssignment = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = excludedId, assignment = "ignored" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, malformedAssignment.StatusCode);

        var missingExclusion = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest(excludedRecordings: []));
        Assert.Equal(HttpStatusCode.BadRequest, missingExclusion.StatusCode);

        var missingTitle = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            new
            {
                reportTitle = (string?)null,
                recordingIdA = fixture.Recordings[0].RecordingId,
                recordingIdB = fixture.Recordings[1].RecordingId,
                metricKey = "rmsAmplitudeDelta",
                signalIdA = fixture.Recordings[0].Signals[0].SignalId,
                signalIdB = fixture.Recordings[1].Signals[0].SignalId,
                excludedRecordings = Array.Empty<object>()
            });
        Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode);
    }

    [Fact]
    public async Task POST_ComparisonReport_FallsBackWhenAiServiceIsUnavailable()
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IComparisonReportNarrativeService>();
            services.AddSingleton<IComparisonReportNarrativeService>(new FailingComparisonNarrativeService());
        }));
        await using var fixture = await ImportComparisonFixture.CreateAsync(factory);

        var response = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/markdown",
            fixture.BuildRequest());

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ExportReportMarkdownResponse>();
        Assert.NotNull(payload);
        Assert.Contains("The aggregate evidence prioritizes", payload!.Markdown);
        Assert.Contains("AI prioritization was unavailable or invalid", payload.Markdown);
        Assert.Contains("rely on the deterministic comparison evidence", payload.Markdown);
        Assert.DoesNotContain("API key test failure", payload.Markdown);
    }

    [Fact]
    public void StructuredNarrative_UsesOnlySelectedBackendFacts()
    {
        var context = CreateNarrativeContext();
        var response = JsonSerializer.Serialize(new
        {
            selectedFactIds = new[]
            {
                "aggregate.crestFactorDelta.compare-b-higher",
                "aggregate.peakAmplitudeDelta.compare-b-higher"
            }
        });

        var result = OpenAiComparisonReportNarrativeService.BuildNarrativeFromSelection(context, response);
        var narrative = string.Join(' ', new[] { result.Overview }.Concat(result.KeyTakeaways).Concat(result.Cautions));

        Assert.False(result.IsFallback);
        Assert.Contains("Aggregate crest factor evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.Contains("Aggregate peak amplitude evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.Contains("selected aligned pair supports the same crest factor direction", result.Overview);
        Assert.DoesNotContain("RMS amplitude", narrative);
        Assert.DoesNotContain("selected aligned pair", string.Join(' ', result.KeyTakeaways), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("limitation", narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"selectedFactIds\":[]}")]
    [InlineData("{\"selectedFactIds\":[\"aggregate.peakAmplitudeDelta.compare-b-higher\"]}")]
    [InlineData("{\"selectedFactIds\":[\"aggregate.crestFactorDelta.compare-b-higher\",\"aggregate.crestFactorDelta.compare-b-higher\"]}")]
    [InlineData("{\"selectedFactIds\":[\"aggregate.rmsAmplitudeDelta.compare-b-higher\"]}")]
    [InlineData("{\"selectedFactIds\":[\"invented.selected.clipping\"]}")]
    [InlineData("{\"selectedFactIds\":[\"aggregate.crestFactorDelta.compare-b-higher\",42]}")]
    public void StructuredNarrative_FallsBackForInvalidFactSelection(string response)
    {
        var result = OpenAiComparisonReportNarrativeService.BuildNarrativeFromSelection(
            CreateNarrativeContext(),
            response);

        Assert.True(result.IsFallback);
        Assert.Contains(result.Cautions, caution =>
            caution.Contains("AI prioritization was unavailable or invalid", StringComparison.Ordinal));
        Assert.Contains("Aggregate crest factor evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.Contains("Aggregate peak amplitude evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.DoesNotContain("RMS amplitude", string.Join(' ', result.KeyTakeaways));
        Assert.DoesNotContain("clipping", result.Overview, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StructuredNarrative_RendersOnlyRealLimitations()
    {
        var result = OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(
            CreateNarrativeContext(includeLimitation: true));

        Assert.Contains(result.Cautions, caution => caution.Contains("reports limitations", StringComparison.Ordinal));
    }

    [Fact]
    public void StructuredNarrative_DoesNotOverstateAnOpposingSelectedPair()
    {
        var context = CreateNarrativeContext(selectedCrestFactorDelta: 0.25);
        var response = "{\"selectedFactIds\":[\"aggregate.crestFactorDelta.compare-b-higher\"]}";

        var result = OpenAiComparisonReportNarrativeService.BuildNarrativeFromSelection(context, response);

        Assert.False(result.IsFallback);
        Assert.Contains("selected aligned pair differs from the aggregate crest factor direction", result.Overview);
        Assert.DoesNotContain("confirm", result.Overview, StringComparison.OrdinalIgnoreCase);
    }

    private static ComparisonReportContext CreateNarrativeContext(
        double selectedCrestFactorDelta = -1.121,
        bool includeLimitation = false)
    {
        var observation = new RecordingComparisonSignalObservation(
            "recording-a:ch:0",
            "Channel 1",
            0,
            "recording-b:ch:0",
            "Channel 1",
            0,
            SignalAlignmentBasis.DisplayName,
            0.65,
            0.81,
            -0.16,
            0.142,
            0.143,
            -0.001,
            5.664,
            6.784,
            selectedCrestFactorDelta,
            0,
            0,
            0,
            false,
            false);
        var aggregates = new[]
        {
            new RecordingComparisonMetricAggregate("crestFactorDelta", "ratio", 2, 0, -0.742, -0.742, -1.121, -0.363, 0.758),
            new RecordingComparisonMetricAggregate("peakAmplitudeDelta", "FS", 2, 0, -0.113, -0.113, -0.156, -0.071, 0.085),
            new RecordingComparisonMetricAggregate("rmsAmplitudeDelta", "FS", 2, 0, -0.001, -0.001, -0.003, 0.001, 0.004),
            new RecordingComparisonMetricAggregate("clippingSampleCountDelta", "samples", 2, 0, 0, 0, 0, 0, 0)
        };
        var limitations = includeLimitation
            ? new[] { new RecordingComparisonLimitation("LowCoverage", "Only limited aligned evidence is available.") }
            : [];
        var comparison = new RecordingComparisonResponse(
            new RecordingComparisonRecording("recording-a", "alpha.wav", 1, 2.5),
            new RecordingComparisonRecording("recording-b", "beta.wav", 1, 2.5),
            [],
            [observation],
            aggregates,
            limitations,
            null);

        return new ComparisonReportContext(
            "Alpha vs beta comparison",
            DateTimeOffset.UtcNow,
            comparison,
            aggregates[0],
            observation,
            []);
    }

    private sealed class StubComparisonNarrativeService : IComparisonReportNarrativeService
    {
        public Task<ReportNarrativeResult> BuildAsync(ComparisonReportContext context, CancellationToken ct) =>
            Task.FromResult(new ReportNarrativeResult(
                "Aggregate RMS evidence is lower for Compare A. The selected aligned pair supports that direction.",
                ["Aggregate evidence is reconstructed from the active comparison."],
                ["Values are normalized to digital full scale."],
                IsFallback: false));
    }

    private sealed class FailingComparisonNarrativeService : IComparisonReportNarrativeService
    {
        public Task<ReportNarrativeResult> BuildAsync(ComparisonReportContext context, CancellationToken ct) =>
            throw new InvalidOperationException("API key test failure");
    }

    private sealed class ImportComparisonFixture : IAsyncDisposable
    {
        private readonly string _directoryPath;
        private readonly IReadOnlyList<string> _paths;

        private ImportComparisonFixture(
            HttpClient client,
            IReadOnlyList<ProbeRecording> recordings,
            string directoryPath,
            IReadOnlyList<string> paths)
        {
            Client = client;
            Recordings = recordings;
            _directoryPath = directoryPath;
            _paths = paths;
        }

        public HttpClient Client { get; }
        public IReadOnlyList<ProbeRecording> Recordings { get; }

        public static async Task<ImportComparisonFixture> CreateAsync(WebApplicationFactory<Program> factory)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), $"soundlens_report_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directoryPath);
            var paths = new[]
            {
                Path.Combine(directoryPath, "alpha.wav"),
                Path.Combine(directoryPath, "beta.wav"),
                Path.Combine(directoryPath, "gamma.wav")
            };
            await File.WriteAllBytesAsync(paths[0], CreateMono16BitWav(8, [4096, 4096, 4096, 4096, 4096, 4096, 4096, 4096]));
            await File.WriteAllBytesAsync(paths[1], CreateMono16BitWav(8, [8192, 8192, 8192, 8192, 8192, 8192, 8192, 8192]));
            await File.WriteAllBytesAsync(paths[2], CreateMono16BitWav(8, [2048, 2048, 2048, 2048, 2048, 2048, 2048, 2048]));

            var client = factory.CreateClient();
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = paths });
            importResponse.EnsureSuccessStatusCode();
            var waveformResponse = await client.PostAsJsonAsync("/api/waveforms/time", new { binCount = 64 });
            waveformResponse.EnsureSuccessStatusCode();
            var imported = await waveformResponse.Content.ReadFromJsonAsync<ImportProbeResponse>();

            return new ImportComparisonFixture(client, imported!.Recordings, directoryPath, paths);
        }

        public object BuildRequest(
            string metricKey = "rmsAmplitudeDelta",
            string? signalIdB = null,
            object[]? excludedRecordings = null,
            bool useRoi = false) => new
        {
            reportTitle = "alpha.wav vs beta.wav comparison",
            recordingIdA = Recordings[0].RecordingId,
            recordingIdB = Recordings[1].RecordingId,
            metricKey,
            signalIdA = Recordings[0].Signals[0].SignalId,
            signalIdB = signalIdB ?? Recordings[1].Signals[0].SignalId,
            excludedRecordings = excludedRecordings ??
            [
                new
                {
                    recordingId = Recordings[2].RecordingId,
                    assignment = "unassigned"
                }
            ],
            startTimeSeconds = useRoi ? 0.0 : (double?)null,
            endTimeSeconds = useRoi ? 0.5 : (double?)null
        };

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            foreach (var path in _paths)
            {
                File.Delete(path);
            }
            Directory.Delete(_directoryPath);

            return ValueTask.CompletedTask;
        }
    }

    private sealed record ImportProbeResponse(IReadOnlyList<ProbeRecording> Recordings);
    private sealed record ProbeRecording(string RecordingId, string FileName, IReadOnlyList<ProbeSignal> Signals);
    private sealed record ProbeSignal(string SignalId);

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
}
