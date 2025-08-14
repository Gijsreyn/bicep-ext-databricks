using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;
using System.Net.Http;
using System.Text.Json;

namespace Bicep.Extension.Databricks.Handlers;

public abstract class BaseHandler<TResource, TIdentifiers> : TypedResourceHandler<TResource, TIdentifiers, Configuration>
    where TResource : class, TIdentifiers
    where TIdentifiers : class
{
    protected readonly ILogger _logger;
    private readonly IDatabricksClientFactory _factory;

    protected BaseHandler(IDatabricksClientFactory factory, ILogger logger)
    {
        _factory = factory;
        _logger = logger;
    }

    protected Task<DatabricksClient> GetClientAsync(string workspaceUrl, CancellationToken ct, int? timeoutSeconds = null)
        => _factory.GetClientAsync(workspaceUrl, ct, timeoutSeconds);

    protected async Task<T?> CallDatabricksApiForResponse<T>(string workspaceUrl, HttpMethod method, string relativePath, CancellationToken ct, object? payload = null, int? timeoutSeconds = null)
    {
        var response = await (_factory as IDatabricksClientFactory).CallApiAsync(workspaceUrl, method, relativePath, ct, payload, timeoutSeconds);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Databricks API call failed: {(int)response.StatusCode} {response.ReasonPhrase} Body={body}");
        }
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
            return default;
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

}
