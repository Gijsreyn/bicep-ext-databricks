using Azure.Identity;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;

namespace Bicep.Extension.Databricks.Handlers;

public abstract class BaseHandler<TResource, TIdentifiers> : TypedResourceHandler<TResource, TIdentifiers, Configuration>
    where TResource : class, TIdentifiers
    where TIdentifiers : class
{
    private readonly HttpClient _httpClient;

    protected BaseHandler()
    {
        _httpClient = new HttpClient();
    }

    protected async Task<string> CallDatabricksApiForResponse(
        ResourceRequest request,
        string apiPath,
        object requestBody,
        CancellationToken cancellationToken)
    {
        string accessToken;
        
        var envToken = Environment.GetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
        {
            accessToken = envToken;
        }
        else
        {
            var credential = new DefaultAzureCredential();
            var tokenResponse = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(["2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default"]),
                cancellationToken);
            accessToken = tokenResponse.Token;
        }

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
}
