using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using Azure.Identity;
using Azure.Core;
using Microsoft.Extensions.Logging;
using Databricks.Models;

namespace Databricks.Handlers;

public abstract class DatabricksResourceHandlerBase<TProps, TIdentifiers>
    : TypedResourceHandler<TProps, TIdentifiers, Configuration>
    where TProps : class
    where TIdentifiers : class
{
    protected readonly ILogger _logger;
    
    protected DatabricksResourceHandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    protected static HttpClient CreateClient(Configuration configuration)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Try DATABRICKS_ACCESS_TOKEN environment variable first
        var accessToken = Environment.GetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        // Fall back to DefaultAzureCredential with Databricks scope
        try
        {
            var credential = DefaultDatabricksCredential.Instance;
            // Databricks Azure resource ID: 2ff814a6-3304-4ab8-85cb-cd0e6f879c1d
            var token = credential.GetToken(new TokenRequestContext(["2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default"]));
            if (string.IsNullOrWhiteSpace(token.Token))
            {
                throw new InvalidOperationException("Empty Azure Entra access token returned for Databricks scope.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return client;
        }
        catch (Exception ex)
        {
            client.Dispose();
            throw new InvalidOperationException("Failed to acquire Databricks credentials. Provide an access token (DATABRICKS_ACCESS_TOKEN) or ensure a federated / managed identity is configured.", ex);
        }
    }

    protected async Task<T?> CallDatabricksApiForResponse<T>(string workspaceUrl, HttpMethod method, string relativePath, CancellationToken ct, object? payload = null, int? timeoutSeconds = null)
    {
        using var client = CreateClient(new Configuration { WorkspaceUrl = workspaceUrl });
        
        var requestUri = new Uri(new Uri(workspaceUrl.TrimEnd('/')), $"api/{relativePath.TrimStart('/')}");
        
        HttpResponseMessage response;
        if (payload is not null)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            if (method == HttpMethod.Post)
                response = await client.PostAsync(requestUri, content, ct);
            else if (method == HttpMethod.Patch)
                response = await PatchAsync(client, requestUri.ToString(), content, ct);
            else if (method == HttpMethod.Put)
                response = await client.PutAsync(requestUri, content, ct);
            else
                throw new InvalidOperationException($"Unsupported HTTP method with payload: {method}");
        }
        else
        {
            if (method == HttpMethod.Get)
                response = await client.GetAsync(requestUri, ct);
            else if (method == HttpMethod.Delete)
                response = await client.DeleteAsync(requestUri, ct);
            else
                throw new InvalidOperationException($"Unsupported HTTP method without payload: {method}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Databricks API call failed: {(int)response.StatusCode} {response.ReasonPhrase} Body={body}");
        }
        
        if (typeof(T) == typeof(object) || response.Content.Headers.ContentLength == 0)
            return default;
            
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // Internal helper to cache DefaultAzureCredential instance (which can be costly to build repeatedly)
    private static class DefaultDatabricksCredential
    {
        internal static readonly DefaultAzureCredential Instance = new(new DefaultAzureCredentialOptions());
    }

    protected static Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, requestUri)
        {
            Content = content
        };
        return client.SendAsync(request, ct);
    }
}
