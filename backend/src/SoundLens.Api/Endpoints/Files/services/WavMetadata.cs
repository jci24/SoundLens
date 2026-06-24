namespace SoundLens.Api.Endpoints.Files.services;

public sealed record WavMetadata(
    int SampleRate,
    int BitDepth,
    int Channels,
    double DurationSeconds,
    string Format,
    long DataSizeBytes,
    ushort AudioFormat
);
