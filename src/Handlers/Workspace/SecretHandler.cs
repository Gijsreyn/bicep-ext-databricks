using Bicep.Local.Extension.Host.Handlers;
using Microsoft.Azure.Databricks.Client;
using Microsoft.Extensions.Logging;
using Bicep.Extension.Databricks.Services;

namespace Bicep.Extension.Databricks.Handlers.Workspace;

public class SecretHandler : BaseHandler<Secret, SecretIdentifiers>
{
    public SecretHandler(IDatabricksClientFactory factory, ILogger<SecretHandler> logger) : base(factory, logger) { }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var desired = request.Properties;
        var client = await GetClientAsync(request.Config.WorkspaceUrl, cancellationToken);

        _logger.LogInformation("Setting secret {Scope}/{Key}", desired.Scope, desired.Key);

        if (!string.IsNullOrEmpty(desired.StringValue))
        {
            await client.Secrets.PutSecret(desired.Scope, desired.Key, desired.StringValue, cancellationToken);
        }
        else if (!string.IsNullOrEmpty(desired.BytesValue))
        {
            await client.Secrets.PutSecret(desired.Scope, desired.Key, desired.BytesValue, cancellationToken);
        }
        else
        {
            throw new ArgumentException("Either StringValue or BytesValue must be provided");
        }

        return GetResponse(request);
    }

    protected override SecretIdentifiers GetIdentifiers(Secret properties) => new() { Scope = properties.Scope, Key = properties.Key };
}
