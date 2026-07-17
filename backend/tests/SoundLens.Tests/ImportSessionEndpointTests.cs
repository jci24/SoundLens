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
}
