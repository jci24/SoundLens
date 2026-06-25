using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using SoundLens.Api.Features.Import.Commands;
using SoundLens.Api.Features.Import.Common;

namespace SoundLens.Tests;

public sealed class ImportFilesHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportFilesHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task POST_Import_StoresFilesInMemory()
    {
        var tempDir = Path.GetTempPath();
        var leftPath = Path.Combine(tempDir, "left_test.wav");
        var rightPath = Path.Combine(tempDir, "right_test.wav");

        await File.WriteAllBytesAsync(leftPath, new byte[4]);
        await File.WriteAllBytesAsync(rightPath, new byte[8]);

        try
        {
            var client = _factory.CreateClient();
            var response = await client.PostAsJsonAsync("/api/import",
                new { filePaths = new[] { leftPath, rightPath } });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ImportFilesResponse>();

            Assert.NotNull(result);
            Assert.Equal(2, result!.SucceededFiles.Count);
            Assert.Empty(result.FailedFiles);
            Assert.Equal(["left_test.wav", "right_test.wav"],
                result.SucceededFiles.Select(f => f.FileName));
        }
        finally
        {
            File.Delete(leftPath);
            File.Delete(rightPath);
        }
    }

    [Fact]
    public async Task POST_Import_FailsForNonExistentFiles()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/import",
            new { filePaths = new[] { "/nonexistent/file.wav" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_Import_RejectsEmptyFilePaths()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/import",
            new { filePaths = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task POST_ImportUpload_StoresUploadedFilesInMemory()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var firstFile = new ByteArrayContent([1, 2, 3, 4]);
        using var secondFile = new ByteArrayContent([5, 6, 7, 8, 9]);

        firstFile.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        secondFile.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        form.Add(firstFile, "files", "alpha.wav");
        form.Add(secondFile, "files", "beta.mp3");

        var response = await client.PostAsync("/api/import/upload", form);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ImportFilesResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result!.SucceededFiles.Count);
        Assert.Empty(result.FailedFiles);
        Assert.Equal(["alpha.wav", "beta.mp3"], result.SucceededFiles.Select(f => f.FileName));
        Assert.All(result.SucceededFiles, file => Assert.True(File.Exists(file.FilePath)));
    }

    [Fact]
    public async Task POST_ImportUpload_RejectsZeroByteFiles()
    {
        var client = _factory.CreateClient();
        using var form = new MultipartFormDataContent();
        using var emptyFile = new ByteArrayContent([]);

        emptyFile.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(emptyFile, "files", "empty.wav");

        var response = await client.PostAsync("/api/import/upload", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
