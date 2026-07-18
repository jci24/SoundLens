using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
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
            var requestedMode = AgentContextModes.Normalize(req.ContextMode);
            var hasExplicitIdentifiers = req.SignalIds is { Count: > 0 } ||
                req.ComparisonContext is not null ||
                req.ComparisonPair is not null;
            var answerMode = requestedMode == AgentContextModes.General
                ? AgentAnswerModes.General
                : requestedMode == AgentContextModes.Auto &&
                  (AgentContextRouter.IsClearlyWebQuestion(req.Question) ||
                   AgentContextRouter.IsClearlyIndustryPracticeQuestion(req.Question))
                    ? AgentAnswerModes.Web
                    : requestedMode == AgentContextModes.Auto &&
                      !hasExplicitIdentifiers &&
                      !AgentContextRouter.IsClearlyWorkspaceQuestion(req.Question)
                        ? AgentAnswerModes.General
                        : AgentAnswerModes.Workspace;
            var isGeneral = answerMode == AgentAnswerModes.General;
            var isWeb = answerMode == AgentAnswerModes.Web;
            await Send.OkAsync(
                new AgentQueryResponse(
                    Answer: MissingApiKeyMessage,
                    CitedEvidence: [],
                    Limitations: isGeneral
                        ? ["No general answer was generated because the OpenAI API key is missing on the backend."]
                        : isWeb
                            ? ["No web research was run because the OpenAI API key is missing on the backend."]
                        :
                        [
                            "Values are in dBFS, not calibrated to physical SPL.",
                            "No grounded investigation was run because the OpenAI API key is missing on the backend."
                        ],
                    NextSteps:
                    [
                        "Set OpenAI:ApiKey in backend configuration or OPENAI__APIKEY in the backend environment.",
                        "Restart the backend and re-run the question."
                    ],
                    ToolsUsed: [],
                    AnswerMode: answerMode),
                ct);
        }
    }
}
