using Azure.Identity;
using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Azure.Databricks.Client;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bicep.Extension.Databricks.Handlers;

public abstract class BaseHandler<TResource, TIdentifiers> : TypedResourceHandler<TResource, TIdentifiers, Configuration>
    where TResource : class, TIdentifiers
    where TIdentifiers : class
{
    private DatabricksClient? _databricksClient;
    private readonly HttpClient _httpClient;

    protected BaseHandler()
    {
        _httpClient = new HttpClient();
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var envToken = Environment.GetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
        {
            Console.WriteLine("[TRACE] Using DATABRICKS_ACCESS_TOKEN from environment variable");
            return envToken;
        }
        else
        {
            Console.WriteLine("[TRACE] Getting token using DefaultAzureCredential");
            var credential = new DefaultAzureCredential();
            var tokenResponse = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(["2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default"]),
                cancellationToken);
            return tokenResponse.Token;
        }
    }

    private async Task<DatabricksClient> GetDatabricksClient(ResourceRequest request, CancellationToken cancellationToken)
    {
        if (_databricksClient != null)
        {
            return _databricksClient;
        }

        var accessToken = await GetAccessTokenAsync(cancellationToken);

        var baseUrl = request.Config.WorkspaceUrl.TrimEnd('/');
        Console.WriteLine($"[TRACE] Creating DatabricksClient for workspace: {baseUrl}");
        
        _databricksClient = DatabricksClient.CreateClient(baseUrl, accessToken);
        return _databricksClient;
    }

    protected async Task<DatabricksClient> GetClientAsync(ResourceRequest request, CancellationToken cancellationToken)
    {
        return await GetDatabricksClient(request, cancellationToken);
    }

    protected async Task<string> CallDatabricksApiForResponse(
        ResourceRequest request,
        string apiPath,
        object requestBody,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var databricksUrl = request.Config.WorkspaceUrl.TrimEnd('/');
        var endpoint = $"{databricksUrl}{apiPath}";

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        if (string.IsNullOrEmpty(json) || json == "{}")
        {
            // For GET requests or requests with empty body, use GET method
            response = await _httpClient.GetAsync(endpoint, cancellationToken);
        }
        else
        {
            // For requests with payload, use POST method
            response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to call Databricks API {apiPath}. Status: {response.StatusCode}, Error: {responseContent}");
        }

        return responseContent;
    }

    protected async Task<ResourceResponse> CallDatabricksApi(
        ResourceRequest request,
        string apiPath,
        object requestBody,
        CancellationToken cancellationToken)
    {
        // Call the API and get the response content
        await CallDatabricksApiForResponse(request, apiPath, requestBody, cancellationToken);

        return GetResponse(request);
    }

    protected ResourceResponse CreateResourceResponse<TDatabricksResult>(
        TDatabricksResult databricksResult, 
        string resourceType,
        Func<TDatabricksResult, TIdentifiers> createIdentifiers,
        Func<TDatabricksResult, TResource> createResource)
    {
        return new ResourceResponse
        {
            Type = resourceType,
            ApiVersion = "2.0",
            Identifiers = createIdentifiers(databricksResult),
            Properties = createResource(databricksResult)
        };
    }
}
