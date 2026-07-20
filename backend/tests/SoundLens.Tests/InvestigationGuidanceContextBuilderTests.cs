using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class InvestigationGuidanceContextBuilderTests
{
    [Fact]
    public void BuildsSafeWorkspaceDescriptorsAndValidatesPairAgainstStore()
    {
        var first = new ImportedFileSummary("baseline.wav", 100, "/private/baseline.wav", "audio/wav");
        var second = new ImportedFileSummary("candidate.wav", 200, "/private/candidate.wav", "audio/wav");
        var store = new InMemoryImportedFileStore();
        store.Replace([first, second]);
        var builder = new InvestigationGuidanceContextBuilder(store, new StubMetadataReader());

        var context = builder.Build(new AgentQueryCommand(
            "How should I analyse these files?",
            SignalIds: ["frontend-authored-signal"],
            StartTimeSeconds: 0.25,
            EndTimeSeconds: 0.75,
            ComparisonContext: new AgentComparisonSelection(
                ImportedFileIdentity.BuildRecordingId(first),
                ImportedFileIdentity.BuildRecordingId(second),
                "rmsAmplitudeDelta",
                "frontend-a",
                "frontend-b")));

        Assert.Equal(2, context.TotalRecordingCount);
        Assert.Equal(["baseline.wav", "candidate.wav"], context.Recordings.Select(item => item.FileName));
        Assert.All(context.Recordings, item =>
        {
            Assert.Equal(2, item.ChannelCount);
            Assert.Equal(1.5, item.DurationSeconds);
        });
        Assert.Equal("baseline.wav", context.CompareAFileName);
        Assert.Equal("candidate.wav", context.CompareBFileName);
        Assert.Equal("ROI from 0.25 s to 0.75 s", context.Scope);
        Assert.Equal(AgentInvestigationPlanScopeKinds.RegionOfInterest, context.PlanScope.Kind);
        Assert.Equal(0.25, context.PlanScope.StartTimeSeconds);
        Assert.Equal(0.75, context.PlanScope.EndTimeSeconds);
        Assert.Equal("RMS amplitude", context.SelectedMetric);
        Assert.DoesNotContain(context.AvailableCapabilities, capability =>
            capability.Description.Contains("spectrogram", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsUnknownPairAndUnsupportedMetricDescriptors()
    {
        var store = new InMemoryImportedFileStore();
        var file = new ImportedFileSummary("baseline.wav", 100, "/private/baseline.wav", "audio/wav");
        store.Replace([file]);
        var builder = new InvestigationGuidanceContextBuilder(store, new StubMetadataReader());

        var context = builder.Build(new AgentQueryCommand(
            "Give me an analysis workflow.",
            SignalIds: null,
            StartTimeSeconds: null,
            EndTimeSeconds: null,
            ComparisonContext: new AgentComparisonSelection(
                ImportedFileIdentity.BuildRecordingId(file),
                "unknown-recording",
                "frontend-invented-metric",
                "signal-a",
                "signal-b")));

        Assert.Null(context.CompareAFileName);
        Assert.Null(context.CompareBFileName);
        Assert.Null(context.SelectedMetric);
        Assert.Equal("Full duration", context.Scope);
        Assert.Equal(AgentInvestigationPlanScopeKinds.FullDuration, context.PlanScope.Kind);
        Assert.DoesNotContain(context.AvailableCapabilities, capability =>
            capability.Id is "evidence_inspector" or "report_export");
    }

    [Fact]
    public void BoundsLargeSessionDescriptorsWhilePreservingTotalCount()
    {
        var files = Enumerable.Range(1, 25)
            .Select(index => new ImportedFileSummary(
                $"recording-{index:00}.wav",
                100 + index,
                $"/private/recording-{index:00}.wav",
                "audio/wav"))
            .ToArray();
        var store = new InMemoryImportedFileStore();
        store.Replace(files);
        var builder = new InvestigationGuidanceContextBuilder(store, new StubMetadataReader());

        var context = builder.Build(new AgentQueryCommand(
            "Give me a workflow for analysing these recordings.",
            null,
            null,
            null));

        Assert.Equal(25, context.TotalRecordingCount);
        Assert.Equal(20, context.Recordings.Count);
        Assert.Equal("recording-20.wav", context.Recordings[^1].FileName);
        Assert.Contains(context.AvailableCapabilities, capability => capability.Id == "waveform");
        Assert.DoesNotContain(context.AvailableCapabilities, capability => capability.Id == "report_export");
    }

    [Fact]
    public void EmptyWorkspaceHasNoCurrentlyAvailableCapabilities()
    {
        var builder = new InvestigationGuidanceContextBuilder(
            new InMemoryImportedFileStore(),
            new StubMetadataReader());

        var context = builder.Build(new AgentQueryCommand(
            "What workflow should I use to analyse product sounds?",
            null,
            null,
            null));

        Assert.Equal(0, context.TotalRecordingCount);
        Assert.Empty(context.AvailableCapabilities);
    }

    private sealed class StubMetadataReader : IImportedRecordingMetadataReader
    {
        public ImportedRecordingInventoryItem Read(ImportedFileSummary file)
        {
            var recordingId = ImportedFileIdentity.BuildRecordingId(file);
            return new ImportedRecordingInventoryItem(
                recordingId,
                file.FileName,
                file.SizeBytes,
                1.5,
                48_000,
                2,
                "discrete multi-channel",
                [
                    new ImportedRecordingSignal($"{recordingId}:ch:0", 0, "Channel 1"),
                    new ImportedRecordingSignal($"{recordingId}:ch:1", 1, "Channel 2")
                ]);
        }
    }
}
