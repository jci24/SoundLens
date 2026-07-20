using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Endpoints;

public sealed class AgentQuery : Endpoint<AgentQueryCommand, AgentQueryResponse>
{
    public override void Configure()
    {
        Post("/agent/query");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Ask the AI copilot a question about the loaded recordings.";
            s.Description = "The Copilot routes general knowledge separately from workspace questions. Workspace answers cite only backend-resolved evidence.";
        });
    }

    public sealed class AgentQueryCommandValidator : Validator<AgentQueryCommand>
    {
        public AgentQueryCommandValidator()
        {
            RuleFor(q => q.Question)
                .NotEmpty()
                .WithMessage("Question is required.")
                .MaximumLength(500)
                .WithMessage("Question must be 500 characters or fewer.");

            RuleFor(q => q.ContextMode)
                .Must(contextMode => AgentContextModes.Normalize(contextMode) is
                    AgentContextModes.Auto or
                    AgentContextModes.Workspace or
                    AgentContextModes.General)
                .WithMessage("ContextMode must be auto, workspace, or general.");

            RuleFor(q => q.ConversationHistory)
                .Must(IsValidConversationHistory)
                .WithMessage(
                    "ConversationHistory must contain at most six valid completed turns and no more than 16,000 characters.");

            RuleFor(q => q)
                .Must(q => AgentContextModes.Normalize(q.ContextMode) == AgentContextModes.General ||
                    (q.StartTimeSeconds is null) == (q.EndTimeSeconds is null))
                .WithMessage("StartTimeSeconds and EndTimeSeconds must be provided together.");

            When(q => AgentContextModes.Normalize(q.ContextMode) != AgentContextModes.General &&
                q.StartTimeSeconds is not null && q.EndTimeSeconds is not null, () =>
            {
                RuleFor(q => q.StartTimeSeconds!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("StartTimeSeconds must be 0 or greater.");

                RuleFor(q => q.EndTimeSeconds!.Value)
                    .GreaterThan(q => q.StartTimeSeconds!.Value)
                    .WithMessage("EndTimeSeconds must be greater than StartTimeSeconds.");
            });

            When(q => AgentContextModes.Normalize(q.ContextMode) != AgentContextModes.General &&
                q.ComparisonContext is not null, () =>
            {
                RuleFor(q => q.ComparisonContext!.RecordingIdA)
                    .NotEmpty()
                    .WithMessage("ComparisonContext.RecordingIdA is required.");

                RuleFor(q => q.ComparisonContext!.RecordingIdB)
                    .NotEmpty()
                    .WithMessage("ComparisonContext.RecordingIdB is required.");

                RuleFor(q => q.ComparisonContext!)
                    .Must(context => !string.Equals(context.RecordingIdA, context.RecordingIdB, StringComparison.Ordinal))
                    .WithMessage("ComparisonContext recording IDs must refer to different recordings.");

                RuleFor(q => q.ComparisonContext!.MetricKey)
                    .NotEmpty()
                    .WithMessage("ComparisonContext.MetricKey is required.")
                    .Must(metricKey => metricKey is
                        "peakAmplitudeDelta" or
                        "rmsAmplitudeDelta" or
                        "crestFactorDelta" or
                        "clippingSampleCountDelta")
                    .WithMessage("ComparisonContext.MetricKey is not supported.");

                RuleFor(q => q.ComparisonContext!.SignalIdA)
                    .NotEmpty()
                    .WithMessage("ComparisonContext.SignalIdA is required.");

                RuleFor(q => q.ComparisonContext!.SignalIdB)
                    .NotEmpty()
                    .WithMessage("ComparisonContext.SignalIdB is required.");
            });

            When(q => AgentContextModes.Normalize(q.ContextMode) != AgentContextModes.General &&
                q.ComparisonPair is not null, () =>
            {
                RuleFor(q => q.ComparisonPair!.RecordingIdA)
                    .NotEmpty()
                    .WithMessage("ComparisonPair.RecordingIdA is required.");

                RuleFor(q => q.ComparisonPair!.RecordingIdB)
                    .NotEmpty()
                    .WithMessage("ComparisonPair.RecordingIdB is required.");

                RuleFor(q => q.ComparisonPair!)
                    .Must(pair => !string.Equals(pair.RecordingIdA, pair.RecordingIdB, StringComparison.Ordinal))
                    .WithMessage("ComparisonPair recording IDs must refer to different recordings.");
            });
        }

        private static bool IsValidConversationHistory(IReadOnlyList<AgentConversationTurn>? history)
        {
            if (history is null)
            {
                return true;
            }

            if (history.Count > 6 ||
                history.Sum(turn => (turn?.Question?.Length ?? 0) + (turn?.Answer?.Length ?? 0)) > 16_000)
            {
                return false;
            }

            return history.All(turn => turn is not null &&
                !string.IsNullOrWhiteSpace(turn.Question) &&
                turn.Question.Length <= 500 &&
                !string.IsNullOrWhiteSpace(turn.Answer) &&
                turn.Answer.Length <= 4_000 &&
                turn.AnswerMode is AgentAnswerModes.Workspace or
                    AgentAnswerModes.General or
                    AgentAnswerModes.Web or
                    AgentAnswerModes.Guidance &&
                turn.RequestSnapshot is not null &&
                IsValidSnapshot(turn.RequestSnapshot));
        }

        private static bool IsValidSnapshot(AgentConversationRequestSnapshot snapshot)
        {
            if (AgentContextModes.Normalize(snapshot.ContextMode) is not (
                AgentContextModes.Auto or AgentContextModes.Workspace or AgentContextModes.General))
            {
                return false;
            }

            if ((snapshot.StartTimeSeconds is null) != (snapshot.EndTimeSeconds is null) ||
                snapshot.StartTimeSeconds is < 0 ||
                snapshot.StartTimeSeconds is not null && snapshot.EndTimeSeconds <= snapshot.StartTimeSeconds)
            {
                return false;
            }

            if (snapshot.SignalIds?.Any(string.IsNullOrWhiteSpace) == true)
            {
                return false;
            }

            if (snapshot.ComparisonContext is { } comparison &&
                (string.IsNullOrWhiteSpace(comparison.RecordingIdA) ||
                 string.IsNullOrWhiteSpace(comparison.RecordingIdB) ||
                 string.Equals(comparison.RecordingIdA, comparison.RecordingIdB, StringComparison.Ordinal) ||
                 comparison.MetricKey is not (
                     "peakAmplitudeDelta" or
                     "rmsAmplitudeDelta" or
                     "crestFactorDelta" or
                     "clippingSampleCountDelta") ||
                 string.IsNullOrWhiteSpace(comparison.SignalIdA) ||
                 string.IsNullOrWhiteSpace(comparison.SignalIdB)))
            {
                return false;
            }

            return snapshot.ComparisonPair is not { } pair ||
                !string.IsNullOrWhiteSpace(pair.RecordingIdA) &&
                !string.IsNullOrWhiteSpace(pair.RecordingIdB) &&
                !string.Equals(pair.RecordingIdA, pair.RecordingIdB, StringComparison.Ordinal);
        }
    }

    public override async Task HandleAsync(AgentQueryCommand req, CancellationToken ct)
    {
        var recorder = new AgentActivityRecorder();
        var command = req with { ActivitySink = recorder };
        try
        {
            var result = await command.ExecuteAsync(ct);
            await Send.OkAsync(result with { ActivityTrace = recorder.Snapshot() }, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            AddUnavailableTrace(recorder);
            var response = AgentUnavailableResponseFactory.ForMissingApiKey(command) with
            {
                ActivityTrace = recorder.Snapshot()
            };
            await Send.OkAsync(response, ct);
        }
    }

    internal static void AddUnavailableTrace(IAgentActivitySink activitySink)
    {
        if (activitySink.Snapshot().Count == 0)
        {
            return;
        }

        activitySink.FailRunning("The configured response service is unavailable.");
        activitySink.AddCompleted(
            AgentActivityKinds.Fallback,
            "Copilot unavailable",
            "The configured response service is unavailable.");
        activitySink.AddCompleted(
            AgentActivityKinds.Completion,
            "Fallback response prepared",
            "A safe availability response is ready.");
    }
}
