using System.Net;
using System.Text;

namespace BonddyPlatform.Tests.TestUtils;

/// <summary>Fake HTTP backend: maps "METHOD /path" to a canned JSON response and records every request.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _routes = new();

    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

    public StubHttpMessageHandler On(HttpMethod method, string path, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes[$"{method} {path}"] = (status, json);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));

        return _routes.TryGetValue($"{request.Method} {request.RequestUri!.AbsolutePath}", out var route)
            ? new HttpResponseMessage(route.Status) { Content = new StringContent(route.Body, Encoding.UTF8, "application/json") }
            : new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
    }
}

public sealed class StubHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler _handler;
    public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
    public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
}
