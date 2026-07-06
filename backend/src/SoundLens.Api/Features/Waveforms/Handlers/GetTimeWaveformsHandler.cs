using FastEndpoints;
using SoundLens.Api.Common;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Waveforms.Commands;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Features.Waveforms.Handlers;

public sealed class GetTimeWaveformsHandler(
    IImportedFileStore importedFileStore,
    IWaveformService waveformService) : CommandHandler<GetTimeWaveformsCommand, TimeWaveformResponse>
{
    public override Task<TimeWaveformResponse> ExecuteAsync(GetTimeWaveformsCommand command, CancellationToken ct = default)
    {
        var currentFiles = importedFileStore.CurrentFiles;
        if (currentFiles.Count == 0)
        {
            ThrowError("Import at least one audio file before requesting waveform data.");
        }

        TimeWaveformResponse response;
        try
        {
            response = waveformService.BuildTimeWaveforms(
                currentFiles,
                command.BinCount,
                command.SignalIds,
                command.StartTimeSeconds,
                command.EndTimeSeconds,
                ct);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            ThrowError(exception.Message);
            throw;
        }

        if (response.Recordings.Count == 0)
        {
            var failedFileMessage = response.FailedFiles.Count > 0
                ? $" Failed files: {string.Join(", ", response.FailedFiles)}."
                : string.Empty;
            ThrowError($"No supported waveform data could be generated. WAV PCM or 32-bit float files are supported in this slice.{failedFileMessage}");
        }

        return Task.FromResult(response);
    }
}
