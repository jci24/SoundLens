using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Handlers;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class AgentActivityRecorderTests
{
    [Fact]
    public void RepeatedMetricActivity_UsesOneAccumulatedSummary()
    {
        Assert.Equal(
            "4 signal metric checks completed.",
            AgentQueryHandler.BuildToolActivitySummary("get_signal_metrics", 4));
        Assert.Equal(
            "Signal comparison completed.",
            AgentQueryHandler.BuildToolActivitySummary("compare_signals", 1));
    }

    [Fact]
    public void BuffersUntilActivatedAndUpdatesOneSequenceInPlace()
    {
        var streamed = new List<AgentActivityEvent>();
        var recorder = new AgentActivityRecorder(activity =>
        {
            streamed.Add(activity);
        });

        var sequence = recorder.Start(
            AgentActivityKinds.Routing,
            "Selecting answer source",
            "Checking available sources.");
        recorder.Complete(sequence, "Using workspace evidence.");

        Assert.Empty(recorder.Snapshot());
        Assert.Empty(streamed);

        recorder.Activate();

        var snapshot = recorder.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal(sequence, snapshot[0].Sequence);
        Assert.Equal(AgentActivityStatuses.Completed, snapshot[0].Status);
        Assert.Single(streamed);

        recorder.Fail(sequence, "Source validation failed.");
        Assert.Single(recorder.Snapshot());
        Assert.Equal(AgentActivityStatuses.Failed, recorder.Snapshot()[0].Status);
        Assert.Equal(2, streamed.Count);
    }

    [Fact]
    public void CapsStepsAndReturnsImmutableSnapshots()
    {
        var recorder = new AgentActivityRecorder(maximumSteps: 2);
        recorder.Activate();
        recorder.AddCompleted(AgentActivityKinds.Plan, "Plan", "Plan ready.");
        recorder.AddCompleted(AgentActivityKinds.Tool, "Tool", "Tool complete.");
        recorder.AddCompleted(AgentActivityKinds.Completion, "Complete", "Response ready.");

        var first = recorder.Snapshot();
        Assert.Equal(2, first.Count);

        recorder.Fail(first[0].Sequence, "Stopped.");
        Assert.Equal(AgentActivityStatuses.Completed, first[0].Status);
        Assert.Equal(AgentActivityStatuses.Failed, recorder.Snapshot()[0].Status);
    }

    [Fact]
    public void FailedStepsUseFailedStatus()
    {
        var recorder = new AgentActivityRecorder();
        recorder.Activate();
        recorder.AddFailed(AgentActivityKinds.Failure, "Stopped", "Could not complete safely.");

        var step = Assert.Single(recorder.Snapshot());
        Assert.Equal(AgentActivityKinds.Failure, step.Kind);
        Assert.Equal(AgentActivityStatuses.Failed, step.Status);
    }
}
