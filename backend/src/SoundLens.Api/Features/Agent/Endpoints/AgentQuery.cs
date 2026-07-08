using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
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
            await Send.ErrorsAsync(503, ct);
        }
    }
}
