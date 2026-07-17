using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class ImportSessionEndpointTests
{
    [Fact]
    public async Task GET_ImportSession_ReturnsEmptySession()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/import/session");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CurrentImportSessionResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Files);
    }

    [Fact]
    public async Task GET_ImportedRecordingInventory_ReturnsOrderedBackendOwnedMetadata()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var monoPath = Path.Combine(Path.GetTempPath(), $"soundlens-inventory-mono-{Guid.NewGuid():N}.wav");
        var stereoPath = Path.Combine(Path.GetTempPath(), $"soundlens-inventory-stereo-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(monoPath, CreatePcmWav(channelCount: 1, sampleRate: 8, frameCount: 8));
        await File.WriteAllBytesAsync(stereoPath, CreatePcmWav(channelCount: 2, sampleRate: 16, frameCount: 32));

        try
        {
            var importResponse = await client.PostAsJsonAsync("/api/import", new
            {
                filePaths = new[] { monoPath, stereoPath },
            });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.GetAsync("/api/import/session/recordings");
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ImportedRecordingInventoryResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            Assert.NotNull(result);
            Assert.Empty(result.FailedFiles);
            Assert.Equal([Path.GetFileName(monoPath), Path.GetFileName(stereoPath)], result.Recordings.Select(item => item.FileName));
            Assert.Equal([1, 2], result.Recordings.Select(item => item.Channels));
            Assert.Equal([8, 16], result.Recordings.Select(item => item.SampleRate));
            Assert.Equal([1.0, 2.0], result.Recordings.Select(item => item.DurationSeconds));
            Assert.Single(result.Recordings[0].Signals);
            Assert.Equal(2, result.Recordings[1].Signals.Count);
            Assert.All(result.Recordings.SelectMany(item => item.Signals), signal =>
                Assert.Contains(":ch:", signal.SignalId, StringComparison.Ordinal));
            Assert.DoesNotContain("filePath", responseText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(monoPath, responseText, StringComparison.Ordinal);
            Assert.DoesNotContain(stereoPath, responseText, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(monoPath);
            File.Delete(stereoPath);
        }
    }

    [Fact]
    public async Task GET_ImportedRecordingInventory_ReportsUnsupportedFilesWithoutInventingMetadata()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var invalidPath = Path.Combine(Path.GetTempPath(), $"soundlens-inventory-invalid-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(invalidPath, [1, 2, 3, 4]);

        try
        {
            var importResponse = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { invalidPath } });
            importResponse.EnsureSuccessStatusCode();

            var result = await client.GetFromJsonAsync<ImportedRecordingInventoryResponse>("/api/import/session/recordings");

            Assert.NotNull(result);
            Assert.Empty(result.Recordings);
            Assert.Equal([Path.GetFileName(invalidPath)], result.FailedFiles);
        }
        finally
        {
            File.Delete(invalidPath);
        }
    }

    [Fact]
    public async Task GET_ImportSession_PreservesOrderWithoutExposingPaths()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        var firstPath = Path.Combine(Path.GetTempPath(), $"soundlens-session-first-{Guid.NewGuid():N}.wav");
        var secondPath = Path.Combine(Path.GetTempPath(), $"soundlens-session-second-{Guid.NewGuid():N}.mp3");
        await File.WriteAllBytesAsync(firstPath, [1, 2, 3]);
        await File.WriteAllBytesAsync(secondPath, [4, 5, 6, 7]);

        try
        {
            var importResponse = await client.PostAsJsonAsync("/api/import", new
            {
                filePaths = new[] { firstPath, secondPath },
            });
            importResponse.EnsureSuccessStatusCode();

            var response = await client.GetAsync("/api/import/session");
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<CurrentImportSessionResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            Assert.NotNull(result);
            Assert.Equal(
                [Path.GetFileName(firstPath), Path.GetFileName(secondPath)],
                result.Files.Select(file => file.FileName));
            Assert.Equal([3L, 4L], result.Files.Select(file => file.SizeBytes));
            Assert.Equal(["audio/wav", "audio/mpeg"], result.Files.Select(file => file.ContentType));
            Assert.DoesNotContain("filePath", responseText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(firstPath, responseText, StringComparison.Ordinal);
            Assert.DoesNotContain(secondPath, responseText, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    private static byte[] CreatePcmWav(short channelCount, int sampleRate, int frameCount)
    {
        const short bitsPerSample = 16;
        var blockAlign = (short)(channelCount * bitsPerSample / 8);
        var dataSize = frameCount * blockAlign;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channelCount);
        writer.Write(sampleRate);
        writer.Write(sampleRate * blockAlign);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);
        return stream.ToArray();
    }
}
