using Microsoft.Azure.Databricks.Client;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Bicep.Extension.Databricks.Services;

public interface IDatabricksClientFactory
{
    Task<DatabricksClient> GetClientAsync(string workspaceUrl, CancellationToken cancellationToken, int? timeoutSeconds = 30);
    Task<HttpResponseMessage> CallApiAsync(string workspaceUrl, HttpMethod method, string relativePath, CancellationToken cancellationToken, object? payload = null, int? timeoutSeconds = null);
}

public class DatabricksClientFactory : IDatabricksClientFactory
{
    private readonly ILogger<DatabricksClientFactory> _logger;
    private readonly ConcurrentDictionary<string, DatabricksClient> _clients = new(StringComparer.OrdinalIgnoreCase);

    public DatabricksClientFactory(ILogger<DatabricksClientFactory> logger)
    {
        _logger = logger;
    }

    public async Task<DatabricksClient> GetClientAsync(string workspaceUrl, CancellationToken cancellationToken, int? timeoutSeconds = null)
    {
        var normalized = workspaceUrl.TrimEnd('/');
        var effectiveTimeout = timeoutSeconds.GetValueOrDefault(30);
        var cacheKey = $"{normalized}||{effectiveTimeout}";
        if (_clients.TryGetValue(cacheKey, out var existing))
        {
            _logger.LogInformation("Reusing client for {Url} (timeout {TimeoutSeconds}s)", normalized, effectiveTimeout);
            return existing;
        }
        var token = await GetAccessTokenAsync(cancellationToken);
        _logger.LogInformation("Creating client for {Url} (timeout {TimeoutSeconds}s)", normalized, effectiveTimeout);
        var created = DatabricksClient.CreateClient(normalized, token, effectiveTimeout);
        _clients[cacheKey] = created;
        return created;
    }

    public async Task<HttpResponseMessage> CallApiAsync(string workspaceUrl, HttpMethod method, string relativePath, CancellationToken cancellationToken, object? payload = null, int? timeoutSeconds = null)
    {
        var normalized = workspaceUrl.TrimEnd('/');
        var effectiveTimeout = timeoutSeconds.GetValueOrDefault(30);
        var token = await GetAccessTokenAsync(cancellationToken);

        // Build minimal HttpClient (not cached to keep method simple; can be optimized later)
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(new Uri(normalized), "api/"),
            Timeout = TimeSpan.FromSeconds(effectiveTimeout)
        };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var request = new HttpRequestMessage(method, relativePath);
        if (payload != null)
        {
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        _logger.LogDebug("Raw Databricks API request {Method} {Path} (timeout {Timeout}s)", method, relativePath, effectiveTimeout);
        var response = await httpClient.SendAsync(request, cancellationToken);
        _logger.LogDebug("Raw Databricks API response {StatusCode} for {Path}", response.StatusCode, relativePath);
        return response;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        // TODO: Identify bug why DATABRICKS_ACCESS_TOKEN doesnt always work
        var envToken = Environment.GetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN", EnvironmentVariableTarget.User);
        if (!string.IsNullOrEmpty(envToken))
        {
            _logger.LogInformation("Using Databricks access token from environment variable DATABRICKS_ACCESS_TOKEN (length {Length})", envToken.Length);
            return envToken;
        }

        const string scope = "2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default";
        _logger.LogInformation("Acquiring Databricks access token via DefaultAzureCredential for scope {Scope}", scope);
        var credential = new DefaultAzureCredential();
        var tokenResponse = await credential.GetTokenAsync(
            new Azure.Core.TokenRequestContext([scope]),
            cancellationToken);
        _logger.LogInformation("Acquired Databricks access token via DefaultAzureCredential (expires {ExpiresOn})", tokenResponse.ExpiresOn);
        return tokenResponse.Token;
    }
}
