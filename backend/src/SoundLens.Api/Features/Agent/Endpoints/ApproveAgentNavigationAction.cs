using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Agent.Endpoints;

public sealed class ApproveAgentNavigationAction(IImportedFileStore importedFileStore)
    : Endpoint<ApproveAgentNavigationActionCommand, AgentNavigationActionResponse>
{
    public override void Configure()
    {
        Post("/agent/actions/navigation");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Validate and approve an allowlisted Sona navigation action.";
            s.Description = "Resolves a closed action ID and current-session prerequisites before navigation.";
        });
    }

    public override async Task HandleAsync(ApproveAgentNavigationActionCommand req, CancellationToken ct)
    {
        if (!AgentNavigationActionCatalog.TryApprove(
                req.ActionId,
                req.CurrentRoute,
                importedFileStore.CurrentFiles.Count > 0,
                out var action,
                out var error))
        {
            AddError(error);
            await Send.ErrorsAsync(409, ct);
            return;
        }

        var activity = new AgentActivityEvent(
            req.PreviousActivitySequence + 1,
            AgentActivityKinds.Action,
            AgentActivityStatuses.Completed,
            "Navigation approved",
            $"Opening {action!.Label.Replace("Open ", string.Empty, StringComparison.Ordinal)}.");
        await Send.OkAsync(new AgentNavigationActionResponse(action.TargetRoute, activity), ct);
    }

    public sealed class Validator : Validator<ApproveAgentNavigationActionCommand>
    {
        public Validator()
        {
            RuleFor(request => request.ActionId).NotEmpty().MaximumLength(64);
            RuleFor(request => request.CurrentRoute)
                .Must(AgentRouteNames.IsSupported)
                .WithMessage("CurrentRoute is not supported.");
            RuleFor(request => request.PreviousActivitySequence).InclusiveBetween(0, 24);
        }
    }
}
