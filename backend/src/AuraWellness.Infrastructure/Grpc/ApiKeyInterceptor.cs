namespace AuraWellness.Infrastructure.Grpc;

/// <summary>
/// HTTP delegating handler that injects the x-internal-key header on every gRPC call.
/// </summary>
public class ApiKeyHandler(string apiKey) : DelegatingHandler
{
    private readonly string _apiKey = apiKey;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Add("x-internal-key", _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
