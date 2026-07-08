using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using SoundLens.Api.Features.Agent.Tools;
using SoundLens.Api.Features.Import.Common;
using SoundLens.Api.Features.Spectra.Common;
using SoundLens.Api.Features.Waveforms.Common;

namespace SoundLens.Api.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application services and configurations here
        services.AddSingleton<IImportedFileStore, InMemoryImportedFileStore>();
        services.AddSingleton<IWaveformService, WaveformService>();
        services.AddSingleton<ISpectrumService, SpectrumService>();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        var openAiModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            services.AddSingleton(new ChatClient(model: openAiModel, apiKey: openAiApiKey));
        }
        else
        {
            // Allow the app to start without an API key in development.
            // The agent endpoint will return a 500 if called without a key configured.
            services.AddSingleton<ChatClient>(_ =>
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable."));
        }

        services.AddSingleton<AgentToolDispatcher>();

        return services;
    }
}
