using System.Net.Http.Json;

namespace Kickstarter.IntegrationTests.Setup;

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PutAsJsonWithDefaultsAsync<T>(
        this HttpClient client,
        string? requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        return client.PutAsJsonAsync(requestUri, value, TestJsonOptions.Default, cancellationToken);
    }

    public static Task<HttpResponseMessage> PostAsJsonWithDefaultsAsync<T>(
        this HttpClient client,
        string? requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        return client.PostAsJsonAsync(requestUri, value, TestJsonOptions.Default, cancellationToken);
    }
}
