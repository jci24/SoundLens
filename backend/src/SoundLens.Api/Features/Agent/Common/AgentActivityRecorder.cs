using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Common;

internal interface IAgentActivitySink
{
    int Start(string kind, string title, string summary);
    void Complete(int sequence, string summary);
    void Fail(int sequence, string summary);
    void AddCompleted(string kind, string title, string summary);
    void AddFailed(string kind, string title, string summary);
    void FailRunning(string summary);
    void Activate();
    IReadOnlyList<AgentActivityEvent> Snapshot();
}

internal sealed class NullAgentActivitySink : IAgentActivitySink
{
    public static NullAgentActivitySink Instance { get; } = new();

    public int Start(string kind, string title, string summary) => 0;
    public void Complete(int sequence, string summary) { }
    public void Fail(int sequence, string summary) { }
    public void AddCompleted(string kind, string title, string summary) { }
    public void AddFailed(string kind, string title, string summary) { }
    public void FailRunning(string summary) { }
    public void Activate() { }
    public IReadOnlyList<AgentActivityEvent> Snapshot() => [];
}

internal sealed class AgentActivityRecorder(
    Action<AgentActivityEvent>? onUpdate = null,
    int maximumSteps = 24) : IAgentActivitySink
{
    private readonly object sync = new();
    private readonly List<AgentActivityEvent> steps = [];
    private int nextSequence = 1;
    private bool isActive;

    public int Start(string kind, string title, string summary)
    {
        AgentActivityEvent? update = null;
        lock (sync)
        {
            if (steps.Count >= maximumSteps)
            {
                return 0;
            }

            update = new AgentActivityEvent(
                nextSequence++, kind, AgentActivityStatuses.Running, title, summary);
            steps.Add(update);
        }

        PublishIfActive(update);
        return update.Sequence;
    }

    public void Complete(int sequence, string summary) =>
        Update(sequence, AgentActivityStatuses.Completed, summary);

    public void Fail(int sequence, string summary) =>
        Update(sequence, AgentActivityStatuses.Failed, summary);

    public void AddCompleted(string kind, string title, string summary)
    {
        var sequence = Start(kind, title, summary);
        Complete(sequence, summary);
    }

    public void AddFailed(string kind, string title, string summary)
    {
        var sequence = Start(kind, title, summary);
        Fail(sequence, summary);
    }

    public void FailRunning(string summary)
    {
        int[] runningSequences;
        lock (sync)
        {
            runningSequences = steps
                .Where(step => step.Status == AgentActivityStatuses.Running)
                .Select(step => step.Sequence)
                .ToArray();
        }

        foreach (var sequence in runningSequences)
        {
            Fail(sequence, summary);
        }
    }

    public void Activate()
    {
        IReadOnlyList<AgentActivityEvent> buffered;
        lock (sync)
        {
            if (isActive)
            {
                return;
            }

            isActive = true;
            buffered = [.. steps];
        }

        foreach (var step in buffered)
        {
            Publish(step);
        }
    }

    public IReadOnlyList<AgentActivityEvent> Snapshot()
    {
        lock (sync)
        {
            return isActive ? [.. steps] : [];
        }
    }

    private void Update(int sequence, string status, string summary)
    {
        if (sequence <= 0)
        {
            return;
        }

        AgentActivityEvent? update = null;
        lock (sync)
        {
            var index = steps.FindIndex(step => step.Sequence == sequence);
            if (index < 0)
            {
                return;
            }

            update = steps[index] with { Status = status, Summary = summary };
            steps[index] = update;
        }

        PublishIfActive(update);
    }

    private void PublishIfActive(AgentActivityEvent update)
    {
        lock (sync)
        {
            if (!isActive)
            {
                return;
            }
        }

        Publish(update);
    }

    private void Publish(AgentActivityEvent update)
    {
        onUpdate?.Invoke(update);
    }
}
