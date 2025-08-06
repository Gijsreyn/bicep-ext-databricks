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

    protected async Task<ResourceResponse> CallDatabricksApi(
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

        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to call Databricks API {apiPath}. Status: {response.StatusCode}, Error: {errorContent}");
        }

        return GetResponse(request);
    }
}
