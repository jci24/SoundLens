using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Api.Features.Spectra.Common;

public interface ISpectrumService
{
    FrequencySpectrumResponse BuildFrequencySpectra(
        IReadOnlyList<ImportedFileSummary> files,
        int requestedBinCount,
        IReadOnlyList<string>? selectedSignalIds,
        CancellationToken cancellationToken);
}
