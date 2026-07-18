using OpenAI.Responses;

namespace SoundLens.Api.Configuration;

#pragma warning disable OPENAI001
public sealed class ResponsesClientProvider(
    IServiceProvider serviceProvider,
    IConfiguration configuration) : IResponsesClientProvider
{
    private const string MissingApiKeyMessage =
        "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable.";

    public string Model => configuration["OpenAI:WebSearchModel"] ?? "gpt-5.6";

    public ResponsesClient GetRequiredClient() =>
        serviceProvider.GetService<ResponsesClient>() ??
        throw new InvalidOperationException(MissingApiKeyMessage);
}
#pragma warning restore OPENAI001
