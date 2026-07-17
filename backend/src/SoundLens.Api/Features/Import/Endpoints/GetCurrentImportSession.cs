using FastEndpoints;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Endpoints;

public sealed class GetCurrentImportSession(IImportedFileStore importedFileStore)
    : EndpointWithoutRequest<CurrentImportSessionResponse>
{
    public override void Configure()
    {
        Get("/import/session");
        Group<ImportGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get the current temporary import session.";
            s.Description = "Returns ordered, browser-safe metadata for files in the current in-memory import session.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var files = importedFileStore.CurrentFiles
            .Select(file => new CurrentImportSessionFile(
                file.FileName,
                file.SizeBytes,
                file.ContentType))
            .ToArray();

        await Send.OkAsync(new CurrentImportSessionResponse(files), ct);
    }
}
