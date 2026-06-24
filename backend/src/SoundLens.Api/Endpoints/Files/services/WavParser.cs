using NAudio.Wave;

namespace SoundLens.Api.Endpoints.Files.services;

public sealed class WavParser
{
    public async Task<WavMetadata> ParseAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new WaveFileReader(stream);

        var waveFormat = reader.WaveFormat;

        return new WavMetadata(
            SampleRate: waveFormat.SampleRate,
            BitDepth: waveFormat.BitsPerSample,
            Channels: waveFormat.Channels,
            DurationSeconds: reader.TotalTime.TotalSeconds,
            Format: waveFormat.Encoding.ToString(),
            DataSizeBytes: reader.Length,
            AudioFormat: (ushort)waveFormat.Encoding
        );
    }
}
