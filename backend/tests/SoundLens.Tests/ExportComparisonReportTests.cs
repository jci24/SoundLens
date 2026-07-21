using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SoundLens.Api.Features.Comparisons.Common;
using SoundLens.Api.Features.Reports.Commands;
using SoundLens.Api.Features.Reports.Common;
using UglyToad.PdfPig;

namespace SoundLens.Tests;

public sealed class ExportComparisonReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportComparisonReportTests(WebApplicationFactory<Program> factory)
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
        Assert.Contains("## Comparison Context", payload.Markdown);
        Assert.Contains("| Check | Status | Detail |", payload.Markdown);
        AssertIntegrityOrder(payload.Markdown);
        Assert.Contains("| Calibration | Unknown |", payload.Markdown);
        Assert.Contains("## Comparison Metrics", payload.Markdown);
        Assert.DoesNotContain("| Rank |", payload.Markdown);
        var peakIndex = payload.Markdown.IndexOf("| Peak amplitude |", StringComparison.Ordinal);
        var rmsIndex = payload.Markdown.IndexOf("| RMS amplitude |", StringComparison.Ordinal);
        var crestIndex = payload.Markdown.IndexOf("| Crest factor |", StringComparison.Ordinal);
        var clippingIndex = payload.Markdown.IndexOf("| Clipping samples |", StringComparison.Ordinal);
        Assert.True(peakIndex < rmsIndex && rmsIndex < crestIndex && crestIndex < clippingIndex);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task POST_ComparisonReport_ReconstructsEvidenceAndExportsPdf(bool useRoi)
    {
        await using var fixture = await ImportComparisonFixture.CreateAsync(_factory);
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/pdf",
            fixture.BuildRequest(useRoi: useRoi));

        response.EnsureSuccessStatusCode();
        var pdf = await response.Content.ReadAsByteArrayAsync();
        var text = ExtractPdfText(pdf);

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'), StringComparison.OrdinalIgnoreCase);
        Assert.True(pdf.AsSpan().StartsWith("%PDF-"u8));
        Assert.Contains("alpha.wav vs beta.wav comparison", text);
        Assert.Contains("Comparison Scope", text);
        Assert.Contains("Compare A alpha.wav", text);
        Assert.Contains("Compare B beta.wav", text);
        Assert.Contains(useRoi ? "0 s to 0.5 s (0.5 s)" : "Full duration", text);
        Assert.Contains("Comparison Context", text);
        AssertIntegrityOrder(text);
        Assert.Contains("Calibration Unknown", text);
        Assert.Contains("Comparison Metrics", text);
        AssertMetricOrder(text);
        Assert.Contains("RMS amplitude", text);
        Assert.Contains("Selected Evidence", text);
        Assert.Contains("Channel 1 vs Channel 1", text);
        Assert.Contains("gamma.wav - Unassigned", text);
        Assert.Contains("not calibrated physical SPL", text);
        Assert.Contains("Traceability", text);
        Assert.Contains(fixture.Recordings[0].RecordingId, text);
        Assert.Contains(fixture.Recordings[1].RecordingId, text);
    }

    [Fact]
    public async Task POST_ComparisonReportPdf_UsesDeterministicFallbackWhenAiFails()
    {
        var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IComparisonReportNarrativeService>();
            services.AddSingleton<IComparisonReportNarrativeService>(new FailingComparisonNarrativeService());
        }));
        await using var fixture = await ImportComparisonFixture.CreateAsync(factory);

        var response = await fixture.Client.PostAsJsonAsync(
            "/api/report/export/comparison/pdf",
            fixture.BuildRequest());

        response.EnsureSuccessStatusCode();
        var text = ExtractPdfText(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("This interpretation describes the selected RMS amplitude evidence.", text);
        Assert.Contains("AI fact selection was unavailable or invalid", text);
        Assert.Contains("rely on the deterministic comparison evidence", text);
        Assert.DoesNotContain("API key test failure", text);
    }

    [Theory]
    [InlineData("markdown")]
    [InlineData("pdf")]
    public async Task POST_ComparisonReport_RejectsInvalidSelectionAndExclusions(string format)
    {
        await using var fixture = await ImportComparisonFixture.CreateAsync(_factory);
        var activeId = fixture.Recordings[0].RecordingId;
        var excludedId = fixture.Recordings[2].RecordingId;
        var endpoint = $"/api/report/export/comparison/{format}";

        var invalidMetric = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(metricKey: "inventedMetric"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidMetric.StatusCode);

        var invalidPair = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(signalIdB: "not-an-aligned-signal"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidPair.StatusCode);

        var duplicateExclusion = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = excludedId, assignment = "A" },
                new { recordingId = excludedId, assignment = "B" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateExclusion.StatusCode);

        var activeExclusion = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = activeId, assignment = "A" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, activeExclusion.StatusCode);

        var unknownExclusion = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = "missing-recording", assignment = "unassigned" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, unknownExclusion.StatusCode);

        var malformedAssignment = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(excludedRecordings:
            [
                new { recordingId = excludedId, assignment = "ignored" }
            ]));
        Assert.Equal(HttpStatusCode.BadRequest, malformedAssignment.StatusCode);

        var missingExclusion = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(excludedRecordings: []));
        Assert.Equal(HttpStatusCode.BadRequest, missingExclusion.StatusCode);

        var missingTitle = await fixture.Client.PostAsJsonAsync(
            endpoint,
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

        var unknownActiveRecording = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(recordingIdA: "missing-recording"));
        Assert.Equal(HttpStatusCode.BadRequest, unknownActiveRecording.StatusCode);

        var malformedRoi = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(startTimeSeconds: 0.5, endTimeSeconds: 0.25));
        Assert.Equal(HttpStatusCode.BadRequest, malformedRoi.StatusCode);

        var unsafeTitle = await fixture.Client.PostAsJsonAsync(
            endpoint,
            fixture.BuildRequest(reportTitle: "unsafe\u0001title"));
        Assert.Equal(HttpStatusCode.BadRequest, unsafeTitle.StatusCode);
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
        Assert.Contains("This interpretation describes the selected RMS amplitude evidence.", payload!.Markdown);
        Assert.Contains("AI fact selection was unavailable or invalid", payload.Markdown);
        Assert.Contains("rely on the deterministic comparison evidence", payload.Markdown);
        Assert.DoesNotContain("API key test failure", payload.Markdown);
    }

    [Fact]
    public void StructuredNarrative_UsesOnlyTheUserSelectedMetric()
    {
        var context = CreateNarrativeContext(selectedMetricKey: "rmsAmplitudeDelta");
        var response = JsonSerializer.Serialize(new
        {
            selectedFactIds = new[] { "aggregate.rmsAmplitudeDelta.compare-b-higher" }
        });

        var result = OpenAiComparisonReportNarrativeService.BuildNarrativeFromSelection(context, response);
        var narrative = string.Join(' ', new[] { result.Overview }.Concat(result.KeyTakeaways).Concat(result.Cautions));

        Assert.False(result.IsFallback);
        Assert.Contains("Aggregate RMS amplitude evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.Contains("selected aligned pair supports the same RMS amplitude direction", result.Overview);
        Assert.DoesNotContain("Crest factor", narrative);
        Assert.DoesNotContain("Peak amplitude", narrative);
        Assert.DoesNotContain("priorit", narrative, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rank", narrative, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("selected aligned pair", string.Join(' ', result.KeyTakeaways), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("limitation", narrative, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComparisonReport_PreservesBackendDomainOrderAcrossIncompatibleUnits()
    {
        var context = CreateNarrativeContext();
        var narrative = OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(context);

        var markdown = ComparisonReportMarkdownWriter.Write(context, narrative);

        var peakIndex = markdown.IndexOf("| Peak amplitude |", StringComparison.Ordinal);
        var rmsIndex = markdown.IndexOf("| RMS amplitude |", StringComparison.Ordinal);
        var crestIndex = markdown.IndexOf("| Crest factor |", StringComparison.Ordinal);
        var clippingIndex = markdown.IndexOf("| Clipping samples |", StringComparison.Ordinal);
        Assert.True(peakIndex < rmsIndex && rmsIndex < crestIndex && crestIndex < clippingIndex);
        Assert.DoesNotContain("| Rank |", markdown);
    }

    [Fact]
    public void ComparisonReportPdf_RendersUnicodeAndPaginatesLongContent()
    {
        var baseContext = CreateNarrativeContext(
            reportTitle: "Måling Ø vs højttaler écho",
            fileNameA: "måling-ø.wav",
            fileNameB: "højttaler-écho.wav");
        var exclusions = Enumerable.Range(1, 80)
            .Select(index => new ComparisonReportExcludedRecording(
                $"excluded-{index}",
                $"måling-{index}-écho.wav",
                "Unassigned"))
            .ToArray();
        var context = baseContext with { ExcludedRecordings = exclusions };
        var pdf = ComparisonReportPdfWriter.Write(
            context,
            OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(context));

        using var document = PdfDocument.Open(pdf);
        var text = string.Join('\n', document.GetPages().Select(ExtractPageText));
        Assert.True(document.NumberOfPages > 1);
        Assert.Contains("Måling Ø vs højttaler écho", text);
        Assert.Contains("måling-ø.wav", text);
        Assert.Contains("højttaler-écho.wav", text);
        Assert.Contains("Metric", text);
    }

    [Fact]
    public void ComparisonReportPdf_KeepsTheLimitationsSectionTogetherAcrossPages()
    {
        var context = CreateNarrativeContext(includeLimitation: true);
        var pdf = ComparisonReportPdfWriter.Write(
            context,
            OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(context));

        using var document = PdfDocument.Open(pdf);
        var pages = document.GetPages().Select(ExtractPageText).ToArray();
        var limitationsPage = Assert.Single(
            pages,
            page => page.Contains("Only limited aligned evidence is available.", StringComparison.Ordinal));

        Assert.True(
            limitationsPage.IndexOf("Limitations", StringComparison.Ordinal) <
            limitationsPage.IndexOf("Only limited aligned evidence is available.", StringComparison.Ordinal));
        Assert.Contains("Only limited aligned evidence is available.", limitationsPage);
        Assert.Contains("not calibrated physical SPL", limitationsPage);
        Assert.Contains("rely on the deterministic comparison evidence", limitationsPage);
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
            caution.Contains("AI fact selection was unavailable or invalid", StringComparison.Ordinal));
        Assert.Contains("Aggregate crest factor evidence is numerically higher for Compare B.", result.KeyTakeaways);
        Assert.DoesNotContain("Aggregate peak amplitude", string.Join(' ', result.KeyTakeaways));
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
    public void StructuredNarrative_AddsCautionForLimitedComparisonContext()
    {
        var context = CreateNarrativeContext();
        var limitedAssessment = context.Comparison.IntegrityAssessment with
        {
            Status = "limited",
            LimitedCheckCount = 1,
            Checks = context.Comparison.IntegrityAssessment.Checks
                .Select(check => check.Code == "DurationScope"
                    ? check with
                    {
                        Status = "limited",
                        Detail = "Full-duration evidence covers unequal durations."
                    }
                    : check)
                .ToArray()
        };
        var limitedContext = context with
        {
            Comparison = context.Comparison with { IntegrityAssessment = limitedAssessment }
        };

        var result = OpenAiComparisonReportNarrativeService.BuildInvalidResponseFallback(limitedContext);
        var markdown = ComparisonReportMarkdownWriter.Write(limitedContext, result);
        var pdfText = ExtractPdfText(ComparisonReportPdfWriter.Write(limitedContext, result));

        Assert.Contains(result.Cautions, caution =>
            caution.Contains("structural limitations", StringComparison.Ordinal));
        Assert.Contains("| Time scope | Review | Full-duration evidence covers unequal durations. |", markdown);
        Assert.Contains("Time scope Review Full-duration evidence covers unequal durations.", pdfText);
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

    [Fact]
    public void StructuredNarrative_DescribesAnEqualSelectedMetricWithoutImportanceClaims()
    {
        var context = CreateNarrativeContext(selectedMetricKey: "clippingSampleCountDelta");
        var response = "{\"selectedFactIds\":[\"aggregate.clippingSampleCountDelta.equal\"]}";

        var result = OpenAiComparisonReportNarrativeService.BuildNarrativeFromSelection(context, response);
        var narrative = string.Join(' ', new[] { result.Overview }.Concat(result.KeyTakeaways));

        Assert.False(result.IsFallback);
        Assert.Contains("Aggregate clipping samples evidence is equal for Compare A and Compare B.", result.KeyTakeaways);
        Assert.DoesNotContain("priorit", narrative, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rank", narrative, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("important", narrative, StringComparison.OrdinalIgnoreCase);
    }

    private static ComparisonReportContext CreateNarrativeContext(
        double selectedCrestFactorDelta = -1.121,
        bool includeLimitation = false,
        string selectedMetricKey = "crestFactorDelta",
        string reportTitle = "Alpha vs beta comparison",
        string fileNameA = "alpha.wav",
        string fileNameB = "beta.wav")
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
            new RecordingComparisonMetricAggregate("peakAmplitudeDelta", "FS", 2, 0, -0.113, -0.113, -0.156, -0.071, 0.085),
            new RecordingComparisonMetricAggregate("rmsAmplitudeDelta", "FS", 2, 0, -0.001, -0.001, -0.003, 0.001, 0.004),
            new RecordingComparisonMetricAggregate("crestFactorDelta", "ratio", 2, 0, -0.742, -0.742, -1.121, -0.363, 0.758),
            new RecordingComparisonMetricAggregate("clippingSampleCountDelta", "samples", 2, 0, 0, 0, 0, 0, 0)
        };
        var limitations = includeLimitation
            ? new[] { new RecordingComparisonLimitation("LowCoverage", "Only limited aligned evidence is available.") }
            : [];
        var comparison = new RecordingComparisonResponse(
            new RecordingComparisonRecording("recording-a", fileNameA, 1, 2.5),
            new RecordingComparisonRecording("recording-b", fileNameB, 1, 2.5),
            [],
            [observation],
            aggregates,
            limitations,
            new RecordingComparisonIntegrityAssessment(
                "complete",
                0,
                1,
                [
                    new RecordingComparisonIntegrityCheck("SampleRate", "matched", "Sample rate", "Both recordings use 44,100 Hz."),
                    new RecordingComparisonIntegrityCheck("DurationScope", "matched", "Time scope", "Both recordings cover the same full duration."),
                    new RecordingComparisonIntegrityCheck("SignalAlignment", "matched", "Signal alignment", "All signal pairs aligned."),
                    new RecordingComparisonIntegrityCheck("Calibration", "unknown", "Calibration", "No validated acoustic calibration is available.")
                ]),
            null);

        var selectedMetric = aggregates.Single(metric => metric.MetricKey == selectedMetricKey);

        return new ComparisonReportContext(
            reportTitle,
            DateTimeOffset.UtcNow,
            comparison,
            selectedMetric,
            observation,
            []);
    }

    private static string ExtractPdfText(byte[] pdf)
    {
        using var document = PdfDocument.Open(pdf);
        return string.Join('\n', document.GetPages().Select(ExtractPageText));
    }

    private static string ExtractPageText(UglyToad.PdfPig.Content.Page page) =>
        string.Join(' ', page.GetWords().Select(word => word.Text));

    private static void AssertMetricOrder(string text)
    {
        var peakIndex = text.IndexOf("Peak amplitude", StringComparison.Ordinal);
        var rmsIndex = text.IndexOf("RMS amplitude", StringComparison.Ordinal);
        var crestIndex = text.IndexOf("Crest factor", StringComparison.Ordinal);
        var clippingIndex = text.IndexOf("Clipping samples", StringComparison.Ordinal);
        Assert.True(peakIndex >= 0 && peakIndex < rmsIndex && rmsIndex < crestIndex && crestIndex < clippingIndex);
    }

    private static void AssertIntegrityOrder(string text)
    {
        var sampleRateIndex = text.IndexOf("Sample rate", StringComparison.Ordinal);
        var timeScopeIndex = text.IndexOf("Time scope", StringComparison.Ordinal);
        var signalAlignmentIndex = text.IndexOf("Signal alignment", StringComparison.Ordinal);
        var calibrationIndex = text.IndexOf("Calibration", StringComparison.Ordinal);
        Assert.True(
            sampleRateIndex >= 0 &&
            sampleRateIndex < timeScopeIndex &&
            timeScopeIndex < signalAlignmentIndex &&
            signalAlignmentIndex < calibrationIndex);
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
            bool useRoi = false,
            string? recordingIdA = null,
            string reportTitle = "alpha.wav vs beta.wav comparison",
            double? startTimeSeconds = null,
            double? endTimeSeconds = null) => new
        {
            reportTitle,
            recordingIdA = recordingIdA ?? Recordings[0].RecordingId,
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
            startTimeSeconds = useRoi ? 0.0 : startTimeSeconds,
            endTimeSeconds = useRoi ? 0.5 : endTimeSeconds
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
