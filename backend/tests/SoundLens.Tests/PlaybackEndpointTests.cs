using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class PlaybackEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlaybackEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPlaybackRecording_ReturnsOriginalBytesWithoutDownloadHeader()
    {
        var fixture = await ImportAsync([1, 2, 3, 4, 5, 6]);

        var response = await fixture.Client.GetAsync($"/api/playback/recordings/{fixture.RecordingId}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("audio/wav", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(6, response.Content.Headers.ContentLength);
        Assert.False(response.Content.Headers.Contains("Content-Disposition"));
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task GetPlaybackRecording_SupportsByteRanges()
    {
        var fixture = await ImportAsync([10, 20, 30, 40, 50, 60]);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/playback/recordings/{fixture.RecordingId}");
        request.Headers.Range = new RangeHeaderValue(2, 4);

        var response = await fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal("bytes", response.Headers.AcceptRanges.Single());
        Assert.Equal("bytes 2-4/6", response.Content.Headers.ContentRange?.ToString());
        Assert.Equal(new byte[] { 30, 40, 50 }, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task GetPlaybackRecording_ReturnsNotFoundForUnknownOrMissingFiles()
    {
        var fixture = await ImportAsync([1, 2, 3]);
        File.Delete(fixture.File.FilePath);

        var missingFileResponse = await fixture.Client.GetAsync($"/api/playback/recordings/{fixture.RecordingId}");
        var unknownResponse = await fixture.Client.GetAsync("/api/playback/recordings/not-a-recording-id");

        Assert.Equal(HttpStatusCode.NotFound, missingFileResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);
    }

    [Fact]
    public async Task GetPlaybackRecording_DoesNotResolveEncodedFilePaths()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/playback/recordings/%2Ftmp%2Fprivate.wav");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<PlaybackFixture> ImportAsync(byte[] bytes)
    {
        var client = _factory.CreateClient();
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens-playback-{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, bytes);

        var response = await client.PostAsJsonAsync("/api/import", new { filePaths = new[] { tempPath } });
        response.EnsureSuccessStatusCode();
        var file = Assert.Single(_factory.Services
            .GetRequiredService<IImportedFileStore>()
            .CurrentFiles);

        return new PlaybackFixture(client, file, ImportedFileIdentity.BuildRecordingId(file));
    }

    private sealed record PlaybackFixture(HttpClient Client, ImportedFileSummary File, string RecordingId);
}
