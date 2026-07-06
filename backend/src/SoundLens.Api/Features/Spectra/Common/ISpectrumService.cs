using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Spectra.Common;

public interface ISpectrumService
{
    FrequencySpectrumResponse BuildFrequencySpectra(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        int? explicitFftSize,
        IReadOnlyList<string>? selectedSignalIds,
        double? startTimeSeconds,
        double? endTimeSeconds,
        CancellationToken cancellationToken);
}
