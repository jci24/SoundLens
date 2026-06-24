using FastEndpoints;
using FluentValidation;
using SoundLens.Api.Features.Import.Commands;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Endpoints;

public sealed class ImportFiles : Endpoint<ImportFilesCommand, ImportFilesResponse>
{
    public override void Configure()
    {
        Post("/import");
        Group<ImportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Import one or more audio files by file path.";
            s.Description = "Accepts an array of local file paths and returns which files were accepted into the current in-memory import session.";
        });
    }

    internal sealed class ImportFilesCommandValidator : Validator<ImportFilesCommand>
    {
        public ImportFilesCommandValidator()
        {
            RuleFor(command => command.FilePaths)
                .NotNull()
                .WithMessage("FilePaths must be provided.")
                .Must(filePaths => filePaths.Count > 0)
                .WithMessage("At least one file path must be provided.");
        }
    }

    public override async Task HandleAsync(ImportFilesCommand req, CancellationToken ct)
    {
        var result = await req.ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
