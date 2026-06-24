using FastEndpoints;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Import.Commands;

public sealed record ImportFilesCommand(IReadOnlyList<string> FilePaths) : ICommand<ImportFilesResponse>;
