using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Waveforms.Common;

public interface IWaveformService
{
    TimeWaveformResponse BuildTimeWaveforms(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        IReadOnlyList<string>? selectedSignalIds,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken cancellationToken);
}
