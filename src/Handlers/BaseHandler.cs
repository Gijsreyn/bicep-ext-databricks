using Azure.Identity;
using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Azure.Databricks.Client;

namespace Bicep.Extension.Databricks.Handlers;

public abstract class BaseHandler<TResource, TIdentifiers> : TypedResourceHandler<TResource, TIdentifiers, Configuration>
    where TResource : class, TIdentifiers
    where TIdentifiers : class
{
    private DatabricksClient? _databricksClient;

    protected BaseHandler()
    {
    }

    private async Task<DatabricksClient> GetDatabricksClient(ResourceRequest request, CancellationToken cancellationToken)
    {
        if (_databricksClient != null)
        {
            return _databricksClient;
        }

        string accessToken;
        
        var envToken = Environment.GetEnvironmentVariable("DATABRICKS_ACCESS_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
        {
            Console.WriteLine("[TRACE] Using DATABRICKS_ACCESS_TOKEN from environment variable");
            accessToken = envToken;
        }
        else
        {
            Console.WriteLine("[TRACE] Getting token using DefaultAzureCredential");
            var credential = new DefaultAzureCredential();
            var tokenResponse = await credential.GetTokenAsync(
                new Azure.Core.TokenRequestContext(["2ff814a6-3304-4ab8-85cb-cd0e6f879c1d/.default"]),
                cancellationToken);
            accessToken = tokenResponse.Token;
        }

        var baseUrl = request.Config.WorkspaceUrl.TrimEnd('/');
        Console.WriteLine($"[TRACE] Creating DatabricksClient for workspace: {baseUrl}");
        
        _databricksClient = DatabricksClient.CreateClient(baseUrl, accessToken);
        return _databricksClient;
    }

    protected async Task<DatabricksClient> GetClientAsync(ResourceRequest request, CancellationToken cancellationToken)
    {
        return await GetDatabricksClient(request, cancellationToken);
    }
}
