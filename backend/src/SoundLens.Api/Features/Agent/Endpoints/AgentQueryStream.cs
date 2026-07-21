using System.Threading.Channels;
using FastEndpoints;
using SoundLens.Api.Features.Agent.Commands;
using SoundLens.Api.Features.Agent.Common;
using SoundLens.Api.Features.Agent.Responses;

namespace SoundLens.Api.Features.Agent.Endpoints;

public sealed class AgentQueryStream : Endpoint<AgentQueryCommand>
{
    private const string SafeFailureMessage = "The investigation could not be completed.";

    public override void Configure()
    {
        Post("/agent/query/stream");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Ask the AI copilot and stream safe investigation activity.";
            s.Description = "Activity is streamed while the final validated answer remains atomic.";
        });
    }

    public override async Task HandleAsync(AgentQueryCommand req, CancellationToken ct)
    {
        HttpContext.Response.OnStarting(() =>
        {
            HttpContext.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        HttpContext.Response.Headers.Append("X-Accel-Buffering", "no");

        var channel = Channel.CreateUnbounded<AgentStreamEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        var recorder = new AgentActivityRecorder(activity =>
        {
            channel.Writer.TryWrite(new AgentStreamEnvelope("activity", Activity: activity));
        });
        var command = req with { ActivitySink = recorder };

        _ = ProduceAsync(command, recorder, channel.Writer, ct);
        await Send.EventStreamAsync("agent-activity", channel.Reader.ReadAllAsync(ct), ct);
    }

    private static async Task ProduceAsync(
        AgentQueryCommand command,
        AgentActivityRecorder recorder,
        ChannelWriter<AgentStreamEnvelope> writer,
        CancellationToken ct)
    {
        try
        {
            AgentQueryResponse response;
            try
            {
                response = await command.ExecuteAsync(ct);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
            {
                AgentQuery.AddUnavailableTrace(recorder);
                response = AgentUnavailableResponseFactory.ForMissingApiKey(command);
            }

            response = AgentResponseActionDecorator.AddSuggestedActions(response, command) with
            {
                ActivityTrace = recorder.Snapshot()
            };
            await writer.WriteAsync(new AgentStreamEnvelope("result", Response: response), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Client cancellation closes the stream without manufacturing a failure response.
        }
        catch
        {
            if (!ct.IsCancellationRequested)
            {
                await writer.WriteAsync(new AgentStreamEnvelope("error", Message: SafeFailureMessage), ct);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }
}
