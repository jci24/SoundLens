using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Endpoints;

public sealed class AgentQuery : Endpoint<AgentQueryCommand, AgentQueryResponse>
{
    private const string MissingApiKeyMessage =
        "Copilot is unavailable because the OpenAI API key is not configured on the backend. Set OpenAI:ApiKey or OPENAI__APIKEY and retry.";

    public override void Configure()
    {
        Post("/agent/query");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Ask the AI copilot a question about the loaded recordings.";
            s.Description = "The agent decides which DSP tools to run, executes them against the backend services, and returns a grounded answer citing only measured evidence.";
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

            RuleFor(q => q)
                .Must(q => (q.StartTimeSeconds is null) == (q.EndTimeSeconds is null))
                .WithMessage("StartTimeSeconds and EndTimeSeconds must be provided together.");

            When(q => q.StartTimeSeconds is not null && q.EndTimeSeconds is not null, () =>
            {
                RuleFor(q => q.StartTimeSeconds!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("StartTimeSeconds must be 0 or greater.");

                RuleFor(q => q.EndTimeSeconds!.Value)
                    .GreaterThan(q => q.StartTimeSeconds!.Value)
                    .WithMessage("EndTimeSeconds must be greater than StartTimeSeconds.");
            });

            When(q => q.ComparisonContext is not null, () =>
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
        }
    }

    public override async Task HandleAsync(AgentQueryCommand req, CancellationToken ct)
    {
        try
        {
            var result = await req.ExecuteAsync(ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            await Send.OkAsync(
                new AgentQueryResponse(
                    Answer: MissingApiKeyMessage,
                    CitedEvidence: [],
                    Limitations:
                    [
                        "Values are in dBFS, not calibrated to physical SPL.",
                        "No grounded investigation was run because the OpenAI API key is missing on the backend."
                    ],
                    NextSteps:
                    [
                        "Set OpenAI:ApiKey in backend configuration or OPENAI__APIKEY in the backend environment.",
                        "Restart the backend and re-run the question."
                    ],
                    ToolsUsed: []),
                ct);
        }
    }
}
