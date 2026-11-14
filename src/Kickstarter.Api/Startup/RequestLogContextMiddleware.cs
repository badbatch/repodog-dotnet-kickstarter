using Microsoft.Extensions.Options;

namespace Kickstarter.Api.Startup;

public sealed class RequestLogContextMiddleware(
    RequestDelegate next,
    ILogger<RequestLogContextMiddleware> logger,
    IOptions<RequestLogContextOptions> options
)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const string ExternalRequestIdHeader = "X-Request-Id";
    private const string FeatureHeader = "X-Feature";
    private const string FeatureQueryParameter = "feature";
    private const int MaximumIdentifierLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetIdentifier(context, CorrelationIdHeader) ?? context.TraceIdentifier;
        var externalRequestId = GetIdentifier(context, ExternalRequestIdHeader);
        var feature = GetIdentifier(context, FeatureHeader) ?? GetQueryIdentifier(context, FeatureQueryParameter);

        var scopeProperties = new Dictionary<string, object?> { ["CorrelationId"] = correlationId };

        if (externalRequestId is not null)
        {
            scopeProperties["ExternalRequestId"] = externalRequestId;
        }

        if (feature is not null)
        {
            scopeProperties["Feature"] = feature;
        }

        foreach (var routeValueName in options.Value.RouteValueProperties)
        {
            if (!context.Request.RouteValues.TryGetValue(routeValueName, out var routeValue))
            {
                continue;
            }

            var routeValueText = routeValue?.ToString();

            if (
                string.IsNullOrWhiteSpace(routeValueText)
                || routeValueText.Length > MaximumIdentifierLength
                || routeValueText.Any(char.IsControl)
            )
            {
                continue;
            }

            scopeProperties[ToPascalCase(routeValueName)] = routeValueText;
        }

        using (logger.BeginScope(scopeProperties))
        {
            await next(context);
        }
    }

    private static string? GetIdentifier(HttpContext context, string headerName)
    {
        if (!context.Request.Headers.TryGetValue(headerName, out var values) || values.Count != 1)
        {
            return null;
        }

        var value = values[0]?.Trim();

        return string.IsNullOrEmpty(value) || value.Length > MaximumIdentifierLength || value.Any(char.IsControl)
            ? null
            : value;
    }

    private static string? GetQueryIdentifier(HttpContext context, string parameterName)
    {
        if (!context.Request.Query.TryGetValue(parameterName, out var values) || values.Count != 1)
        {
            return null;
        }

        var value = values[0]?.Trim();

        return string.IsNullOrEmpty(value) || value.Length > MaximumIdentifierLength || value.Any(char.IsControl)
            ? null
            : value;
    }

    private static string ToPascalCase(string value)
    {
        return string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }
}

public sealed class RequestLogContextOptions
{
    public string[] RouteValueProperties { get; set; } = [];
}
