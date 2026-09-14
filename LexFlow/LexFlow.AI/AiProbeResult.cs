using System.Net;
using System.Net.Sockets;

namespace LexFlow.AI;

public enum AiProbeStatus
{
    Ok,
    InvalidCredential,
    Unreachable,
    Error
}

public sealed record AiProbeResult(AiProbeStatus Status, string Message)
{
    public bool Succeeded => Status == AiProbeStatus.Ok;

    public static AiProbeResult Ok(string message = "Connected")
        => new(AiProbeStatus.Ok, message);

    public static AiProbeResult InvalidCredential(string? message = null)
        => new(AiProbeStatus.InvalidCredential,
            message ?? "This API key was rejected. Check that you copied the full key.");

    public static AiProbeResult Unreachable(string? message = null)
        => new(AiProbeStatus.Unreachable,
            message ?? "Could not reach the provider. Check your connection and try again.");

    public static AiProbeResult Error(string message)
        => new(AiProbeStatus.Error, message);

    public static AiProbeResult FromHttpStatus(HttpStatusCode statusCode, bool treatBadRequestAsInvalidKey = false)
    {
        var code = (int)statusCode;
        if (code is >= 200 and < 300 or 429)
        {
            return Ok();
        }

        if (code is 401 or 403 || (treatBadRequestAsInvalidKey && code == 400))
        {
            return InvalidCredential();
        }

        return Error($"The provider returned HTTP {code}.");
    }

    public static AiProbeResult FromException(Exception exception)
    {
        if (exception is HttpRequestException or SocketException)
        {
            return Unreachable();
        }

        if (exception is TaskCanceledException or OperationCanceledException or TimeoutException)
        {
            return Unreachable("The provider did not respond in time. Check your connection and try again.");
        }

        return Error(exception.Message);
    }
}
