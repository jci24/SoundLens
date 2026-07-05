namespace SoundLens.Api.Features.Spectra.Common;

public static class FrequencySpectrumOptions
{
    public const int MinimumBinCount = 129;
    public const int DefaultBinCount = 22051;
    public const int MaximumBinCount = 32769;

    public const int DefaultFftSize = 44100;

    public static readonly IReadOnlySet<int> AllowedFftSizes = new HashSet<int>
    {
        256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 44100, 65536,
    };
}
