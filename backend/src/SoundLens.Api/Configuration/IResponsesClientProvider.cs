using OpenAI.Responses;

namespace SoundLens.Api.Configuration;

#pragma warning disable OPENAI001
public interface IResponsesClientProvider
{
    ResponsesClient GetRequiredClient();
    string Model { get; }
}
#pragma warning restore OPENAI001
