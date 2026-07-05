using FastEndpoints;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Commands;
using SoundLens.Api.Features.Spectra.Common;

namespace SoundLens.Api.Features.Spectra.Handlers;

public sealed class GetFrequencySpectraHandler(
    IImportedFileStore importedFileStore,
    ISpectrumService spectrumService) : CommandHandler<GetFrequencySpectraCommand, FrequencySpectrumResponse>
{
    public override Task<FrequencySpectrumResponse> ExecuteAsync(
        GetFrequencySpectraCommand command,
        CancellationToken ct = default)
    {
        var currentFiles = importedFileStore.CurrentFiles;
        if (currentFiles.Count == 0)
        {
            ThrowError("Import at least one audio file before requesting spectrum data.");
        }

        var response = spectrumService.BuildFrequencySpectra(currentFiles, command.BinCount, command.FftSize, command.SignalIds, ct);

        if (response.Recordings.Count == 0)
        {
            var failedFileMessage = response.FailedFiles.Count > 0
                ? $" Failed files: {string.Join(", ", response.FailedFiles)}."
                : string.Empty;
            ThrowError($"No supported spectrum data could be generated. WAV PCM or 32-bit float files are supported in this slice.{failedFileMessage}");
        }

        return Task.FromResult(response);
    }
}
