using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Net.Http.Headers;

namespace SoundLens.Api.Common;

internal static class NegotiatedBinaryResponse
{
    internal const string MessagePackContentType = "application/x-msgpack";
    private const string AlternateMessagePackContentType = "application/msgpack";

    private static readonly MessagePackSerializerOptions SerializerOptions =
        MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

    internal static bool ShouldUseMessagePack(HttpRequest request)
    {
        var acceptedMediaTypes = request.GetTypedHeaders().Accept;

        if (acceptedMediaTypes is null)
        {
            return false;
        }

        return acceptedMediaTypes.Any(header =>
            header.MediaType.HasValue &&
            (header.MediaType.Value.Equals(MessagePackContentType, StringComparison.OrdinalIgnoreCase) ||
             header.MediaType.Value.Equals(AlternateMessagePackContentType, StringComparison.OrdinalIgnoreCase)));
    }

    internal static async Task SendMessagePackAsync<T>(HttpResponse response, T payload, CancellationToken cancellationToken)
    {
        var bytes = MessagePackSerializer.Serialize(payload, SerializerOptions, cancellationToken);

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = MessagePackContentType;
        response.ContentLength = bytes.Length;
        response.Headers[HeaderNames.Vary] = HeaderNames.Accept;

        await response.Body.WriteAsync(bytes, cancellationToken);
    }
}
