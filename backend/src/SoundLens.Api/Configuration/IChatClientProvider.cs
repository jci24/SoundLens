using OpenAI.Chat;

namespace SoundLens.Api.Configuration;

public interface IChatClientProvider
{
    ChatClient GetRequiredClient();
}
