namespace Wormholio;

public sealed class EsiMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // TODO: Implement caching
        throw new NotImplementedException();
    }
}