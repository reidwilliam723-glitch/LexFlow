using System.Net;
using System.Net.Http;

namespace LexFlow.AI;

internal static class AiHttpCall
{
    public static Task<(HttpResponseMessage? Response, Exception? Error)> PostAsync(
        HttpClient client,
        string url,
        HttpContent content,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => SendAsync(client, new HttpRequestMessage(HttpMethod.Post, url) { Content = content }, timeout, cancellationToken);

    public static Task<(HttpResponseMessage? Response, Exception? Error)> GetAsync(
        HttpClient client,
        string url,
        TimeSpan timeout,
        CancellationToken cancellationToken)
        => SendAsync(client, new HttpRequestMessage(HttpMethod.Get, url), timeout, cancellationToken);

    private static async Task<(HttpResponseMessage? Response, Exception? Error)> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return (response, null);
        }
        catch (Exception ex)
        {
            request.Dispose();
            return (null, ex);
        }
    }
}
