using OpenAI.Chat;

namespace SoundLens.Api.Configuration;

public sealed class ChatClientProvider(IServiceProvider serviceProvider) : IChatClientProvider
{
    private const string MissingApiKeyMessage =
        "OpenAI API key is not configured. Set OpenAI:ApiKey in appsettings or the OPENAI__APIKEY environment variable.";

    public ChatClient GetRequiredClient()
    {
        try
        {
            return serviceProvider.GetService<ChatClient>()
                ?? throw new InvalidOperationException(MissingApiKeyMessage);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key", StringComparison.OrdinalIgnoreCase))
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            throw new InvalidOperationException(MissingApiKeyMessage, ex);
        }
    }
}
