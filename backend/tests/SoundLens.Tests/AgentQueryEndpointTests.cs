using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Tests;

public sealed class AgentQueryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AgentQueryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ChatClient>();
                services.AddSingleton<ChatClient>(_ =>
                    throw new InvalidOperationException(
                        "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable."));
            });
        });
    }

    [Fact]
    public async Task ReturnsStructuredUnavailableResponseWhenApiKeyIsMissing()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"soundlens_agent_query_{Guid.NewGuid():N}.wav");
        await File.WriteAllBytesAsync(tempPath, CreateMono16BitWav(
            sampleRate: 4,
            samples: [-32768, 32767, -16384, 16384]));

        using var client = _factory.CreateClient();
        try
        {
            var importResponse = await client.PostAsJsonAsync(
                "/api/import",
                new
                {
                    filePaths = new[] { tempPath }
                });

            importResponse.EnsureSuccessStatusCode();

            var response = await client.PostAsJsonAsync(
                "/api/agent/query",
                new
                {
                    question = "Which signal is louder?"
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var payload = await response.Content.ReadFromJsonAsync<AgentQueryResponse>();
            Assert.NotNull(payload);
            Assert.Contains("not configured", payload!.Answer, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(payload.CitedEvidence);
            Assert.Empty(payload.ToolsUsed);
            Assert.Contains(payload.Limitations, item => item.Contains("dBFS", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(payload.NextSteps, item => item.Contains("OPENAI__APIKEY", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static byte[] CreateMono16BitWav(int sampleRate, IReadOnlyList<short> samples)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        var bytesPerSample = bitsPerSample / 8;
        var byteRate = sampleRate * channels * bytesPerSample;
        var blockAlign = (short)(channels * bytesPerSample);
        var dataSize = samples.Count * bytesPerSample;
        var riffChunkSize = 36 + dataSize;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(riffChunkSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }

        writer.Flush();
        return stream.ToArray();
    }
}
